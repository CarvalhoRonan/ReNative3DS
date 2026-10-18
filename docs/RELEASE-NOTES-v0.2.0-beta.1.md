# ReNative3D36S v0.2.0-beta.1: pré-lançamento para testes

Este é o primeiro release público da interface com **seleção de motores** e correção visual dos dropdowns. **Versão experimental**, sem teste completo de compilação de ROM e execução no R36S confirmado para esta revisão.

## Destaques

- Seleção e explicação de status dos motores: Pocket Native Oficial, fork compatível local, Zakuro/3dsrecomp Windows e ReNative Optimized (desabilitado, ainda não implementado).
- Interface WPF azul-escuro com suporte a PT-BR, EN, ES e RU.
- Correção de contraste das listas suspensas de motor e idioma.
- Tela **Sobre/Apoiar** organizada com redes sociais e QR codes primeiro, apoio voluntário Pix/Ko-fi, informações e créditos dos motores.
- Verificação de versões do Pocket Native, logs, saída individual por jogo, tutorial de Docker/WSL2 e compartilhamento de pastas.

## Downloads

- **Source code (zip/tar.gz)**: gerado automaticamente pelo GitHub; inclui código-fonte, documentação e `COMPILAR-UM-CLIQUE.cmd`.
- **ReNative3D36S-Windows-x64-TESTE.zip**: anexado somente se o build automático do GitHub Actions for concluído com sucesso. Não é considerado estável.

**Windows:** execute `COMPILAR-UM-CLIQUE.cmd`; depois abra `dist/portable/ReNative3D36S.exe`. Para recompilar ROMs, inicie o Docker Desktop antes e permita acesso às pastas selecionadas.

**Atenção:** o programa não distribui ROMs, firmware ou chaves. Binários derivados de jogos comerciais não devem ser enviados ao repositório ou releases.

Em caso de falha, consulte nosso [tutorial](https://github.com/CarvalhoRonan/ReNative3DS/blob/main/docs/TUTORIAL-PT-BR.md) e as [instruções originais do Pocket Native](https://github.com/whovian64/pocket-native/blob/master/START_HERE.md).

## Credits / Credits

GUI: Overclock Informática, MIT. Pocket Native: whovian / fearkov (MIT). Zakuro and 3dsrecomp: fearkov (MIT). Third-party software retains its own licenses. [Avisos](https://github.com/CarvalhoRonan/ReNative3DS/blob/main/THIRD_PARTY_NOTICES.md).
