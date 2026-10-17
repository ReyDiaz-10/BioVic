using System.Security.Cryptography;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BioVic.Infrastructure.Services;

public sealed class EquipmentDocumentService(
    BioVicDbContext db,
    IAuditService audit,
    LocationAccessService locationAccess,
    IConfiguration configuration) : IEquipmentDocumentService
{
    private static readonly HashSet<string> AllowedTypes = EquipmentDocumentTypes.All.ToHashSet(StringComparer.Ordinal);
    private readonly long _maxBytes = ParseMaxBytes(configuration["BIOVIC_DOCUMENT_MAX_BYTES"]);
    private readonly string _storageRoot = ResolveStorageRoot(configuration["BIOVIC_DOCUMENT_PATH"]);

    public async Task<IReadOnlyList<EquipmentDocumentDto>> ListAsync(Guid equipmentId, RequestContext actor,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (includeDeleted && actor.Role != UserRole.MasterAdministrator)
            throw new ForbiddenException("Solo el Administrador Maestro puede consultar documentos archivados.");

        IQueryable<EquipmentDocument> query = includeDeleted
            ? db.EquipmentDocuments.IgnoreQueryFilters()
            : db.EquipmentDocuments;
        var items = await query.AsNoTracking().Where(item => item.EquipmentId == equipmentId)
            .OrderByDescending(item => item.IsCurrent).ThenBy(item => item.DocumentType)
            .ThenByDescending(item => item.Version).ThenByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<EquipmentDocumentDto> UploadAsync(Guid equipmentId, UploadEquipmentDocumentRequest request,
        Stream content, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (!AllowedTypes.Contains(request.DocumentType))
            throw new ValidationException("El tipo de documento no está permitido.");
        if (request.ExpiresAtUtc is not null && request.DocumentDateUtc is not null
            && request.ExpiresAtUtc <= request.DocumentDateUtc)
            throw new ValidationException("La fecha de vencimiento debe ser posterior a la fecha del documento.");

        var originalName = Path.GetFileName(request.OriginalName?.Trim());
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 220)
            throw new ValidationException("El nombre del archivo no es válido.");
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (extension is not (".pdf" or ".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new ValidationException("Sólo se admiten PDF, PNG, JPG y WEBP.");

        EquipmentDocument? superseded = null;
        if (request.SupersedesDocumentId is Guid supersededId)
        {
            superseded = await db.EquipmentDocuments.SingleOrDefaultAsync(
                item => item.Id == supersededId && item.EquipmentId == equipmentId, cancellationToken)
                ?? throw new NotFoundException("No se encontró la versión anterior del documento.");
            if (!superseded.IsCurrent)
                throw new ValidationException("Seleccione la versión vigente para crear una nueva versión.");
        }

        Directory.CreateDirectory(_storageRoot);
        var relativeDirectory = Path.Combine(equipmentId.ToString("N"), DateTimeOffset.UtcNow.ToString("yyyy"));
        var absoluteDirectory = SafeCombine(_storageRoot, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);
        var storageName = Guid.NewGuid().ToString("N") + extension;
        var relativePath = Path.Combine(relativeDirectory, storageName).Replace('\\', '/');
        var finalPath = SafeCombine(_storageRoot, relativePath);
        var temporaryPath = finalPath + ".upload";

        long size = 0;
        string sha256;
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             81_920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81_920];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    size += read;
                    if (size > _maxBytes)
                        throw new ValidationException($"El archivo supera el límite de {_maxBytes / 1_048_576} MB.");
                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            if (size == 0) throw new ValidationException("El archivo está vacío.");
            ValidateSignature(temporaryPath, extension);
            sha256 = Convert.ToHexString(hash.GetHashAndReset());
            File.Move(temporaryPath, finalPath, false);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }

        var entity = new EquipmentDocument
        {
            EquipmentId = equipmentId,
            OriginalName = originalName,
            DocumentType = request.DocumentType,
            ContentType = ContentType(extension),
            Description = Clean(request.Description),
            RelativePath = relativePath,
            Sha256 = sha256,
            SizeBytes = size,
            Version = superseded?.Version + 1 ?? 1,
            IsCurrent = true,
            SupersedesDocumentId = superseded?.Id,
            DocumentDateUtc = request.DocumentDateUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            ApprovedForAiKnowledge = request.ApprovedForAiKnowledge,
            CreatedByUserId = actor.UserId
        };

        try
        {
            if (superseded is not null)
            {
                superseded.IsCurrent = false;
                superseded.UpdatedAtUtc = DateTimeOffset.UtcNow;
                superseded.UpdatedByUserId = actor.UserId;
            }
            db.EquipmentDocuments.Add(entity);
            await audit.AppendAsync(new(actor, superseded is null ? "DOCUMENT_UPLOADED" : "DOCUMENT_VERSION_UPLOADED",
                "EquipmentDocument", entity.Id.ToString("D"), equipmentId,
                $"Se guardó {entity.DocumentType}: {entity.OriginalName}, versión {entity.Version}, SHA-256 {entity.Sha256}."), cancellationToken);

            return Map(entity);
        }
        catch
        {

            if (File.Exists(finalPath)) File.Delete(finalPath);
            throw;
        }
    }

    public async Task<EquipmentDocumentDownloadDto> DownloadAsync(Guid equipmentId, Guid documentId,
        RequestContext actor, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var entity = await db.EquipmentDocuments.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == documentId && item.EquipmentId == equipmentId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el documento.");
        var path = SafeCombine(_storageRoot, entity.RelativePath);
        if (!File.Exists(path)) throw new NotFoundException("El archivo físico no está disponible. Informe al administrador.");
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(entity.Sha256), Convert.FromHexString(actualHash)))
            throw new InvalidOperationException("El documento no superó la verificación de integridad SHA-256.");
        await audit.AppendAsync(new(actor, "DOCUMENT_DOWNLOADED", "EquipmentDocument", entity.Id.ToString("D"),
            equipmentId, $"Se descargó {entity.OriginalName}, versión {entity.Version}."), cancellationToken);
        return new(entity.OriginalName, entity.ContentType, bytes, entity.Sha256);
    }

    public async Task ArchiveAsync(Guid equipmentId, Guid documentId, ArchiveDocumentRequest request,
        RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Indique el motivo del archivo lógico.");
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var entity = await db.EquipmentDocuments.SingleOrDefaultAsync(
            item => item.Id == documentId && item.EquipmentId == equipmentId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el documento.");
        entity.IsDeleted = true;
        entity.IsCurrent = false;
        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = actor.UserId;
        entity.DeletionReason = request.Reason.Trim();
        if (entity.SupersedesDocumentId is Guid previousId)
        {
            var previous = await db.EquipmentDocuments.SingleOrDefaultAsync(item => item.Id == previousId, cancellationToken);
            if (previous is not null) previous.IsCurrent = true;
        }
        await audit.AppendAsync(new(actor, "DOCUMENT_ARCHIVED", "EquipmentDocument", entity.Id.ToString("D"), equipmentId,
            $"Se archivó lógicamente {entity.OriginalName}. Motivo: {entity.DeletionReason}"), cancellationToken);
    }

    public async Task<EquipmentDocumentDto> RestoreAsync(Guid equipmentId, Guid documentId, RestoreDocumentRequest request,
        RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Indique el motivo de restauración.");
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var entity = await db.EquipmentDocuments.IgnoreQueryFilters().SingleOrDefaultAsync(
            item => item.Id == documentId && item.EquipmentId == equipmentId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el documento archivado.");
        if (!entity.IsDeleted) throw new ValidationException("El documento ya está activo.");
        if (!File.Exists(SafeCombine(_storageRoot, entity.RelativePath)))
            throw new NotFoundException("No puede restaurarse porque falta el archivo físico.");
        entity.IsDeleted = false;
        entity.DeletedAtUtc = null;
        entity.DeletedByUserId = null;
        entity.DeletionReason = null;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actor.UserId;
        entity.IsCurrent = !await db.EquipmentDocuments.AnyAsync(
            item => item.EquipmentId == equipmentId && item.SupersedesDocumentId == entity.Id, cancellationToken);
        await audit.AppendAsync(new(actor, "DOCUMENT_RESTORED", "EquipmentDocument", entity.Id.ToString("D"), equipmentId,
            $"Se restauró {entity.OriginalName}. Motivo: {request.Reason.Trim()}"), cancellationToken);
        return Map(entity);
    }

    private static EquipmentDocumentDto Map(EquipmentDocument item) => new(item.Id, item.EquipmentId,
        item.OriginalName, item.DocumentType, item.ContentType, item.Description, item.SizeBytes, item.Sha256,
        item.Version, item.IsCurrent, item.SupersedesDocumentId, item.DocumentDateUtc, item.ExpiresAtUtc,
        item.ApprovedForAiKnowledge, item.CreatedAtUtc, item.IsDeleted, item.DeletedAtUtc, item.DeletionReason);

    private static void EnsureEditor(RequestContext actor)
    {
        if (actor.UserId is null || actor.Role is not (UserRole.Editor or UserRole.MasterAdministrator))
            throw new ForbiddenException("Esta acción requiere Editor o Administrador Maestro.");
    }

    private static void EnsureMaster(RequestContext actor)
    {
        if (actor.UserId is null || actor.Role != UserRole.MasterAdministrator)
            throw new ForbiddenException("Esta acción requiere Administrador Maestro.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static long ParseMaxBytes(string? raw) => long.TryParse(raw, out var value) && value is >= 1_048_576 and <= 52_428_800
        ? value : 20_971_520;
    private static string ResolveStorageRoot(string? configured)
    {
        var value = string.IsNullOrWhiteSpace(configured)
            ? (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"))
                ? Path.Combine("data", "documents") : "/home/data/biovic-documents")
            : configured.Trim();
        return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value));
    }

    private static string SafeCombine(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidOperationException("Ruta de documento no válida.");
        return full;
    }

    private static string ContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static void ValidateSignature(string path, string extension)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = File.OpenRead(path);
        var read = stream.Read(header);
        var valid = extension switch
        {
            ".pdf" => read >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => read >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
        if (!valid) throw new ValidationException("El contenido del archivo no coincide con su extensión.");
    }
}

