# Publicação oficial no GitHub

Repositório: https://github.com/CarvalhoRonan/ReNative3DS

A pasta principal tem `COMPILAR-UM-CLIQUE.cmd`, que publica o programa Windows x64 a partir do código-fonte. O GitHub Actions em `.github/workflows/windows-build.yml` também faz a publicação automática.

**Release experimental**: na primeira alteração enviada à branch `main`, o workflow tenta criar `v0.2.0-beta.1` com código-fonte automático. Depois do build Windows, se aprovado, anexa `ReNative3D36S-Windows-x64-TESTE.zip`.

Para baixar o fonte: https://github.com/CarvalhoRonan/ReNative3DS/releases

Não inclua ROMs, chaves AES, firmware, saves ou arquivos particulares nos commits ou nas Issues. Preserve as licenças dos componentes externos, listadas em `THIRD_PARTY_NOTICES.md`.

Caso o workflow de release falhe por permissões, confira Settings → Actions → General → Workflow permissions e autorize `Read and write permissions` conforme as políticas do repositório. Reexecute a etapa após revisar os logs.

Antes de considerar estável, valide o `.exe` no Windows e a execução de pelo menos um jogo recompilado no R36S. Nunca trate a existência do ZIP como validação funcional.
