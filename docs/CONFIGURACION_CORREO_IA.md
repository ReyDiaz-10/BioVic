# Configuración de IA

BioVic no envía hojas de vida directamente por correo. El usuario puede descargar el PDF y adjuntarlo desde su proveedor de correo habitual. No se requieren variables ni credenciales SMTP.

## Ollama

Para un servidor Ollama local:

```env
BIOVIC_AI_ENABLED=true
BIOVIC_AI_PROVIDER=ollama
BIOVIC_AI_BASE_URL=http://localhost:11434
BIOVIC_AI_MODEL=qwen3:8b
BIOVIC_AI_API_KEY=
```

Si el proveedor de Ollama exige una clave, se registra en `BIOVIC_AI_API_KEY`. BioVic no incluye esta clave en las solicitudes de auditoría ni en los archivos de registro.
