# Motores do ReNative3D36S v0.2.0 (versão de testes)

Acesse a aba **Motores**. O status apresentado é por **configuração da integração**, não é um teste automático de funcionamento do jogo.

## 1. Pocket Native Oficial

- Estado: adaptador oficial já existente no projeto. Requer Docker Desktop/Podman e arquivos de jogo próprios.
- Destino: R36S / ArkOS ARM64. Nem todos os jogos funcionam.
- Se o Docker solicitar acesso à unidade do jogo (por exemplo `E:`) ou do destino, autorize. Abra o Docker Desktop e aguarde ele terminar de iniciar ANTES de recompilar.
- Ajuda do autor: https://github.com/whovian64/pocket-native/blob/master/START_HERE.md

## 2. ReNative Optimized

- Estado: **em desenvolvimento e desativado**. Não existe um motor otimizado nosso verificado para distribuição.
- Não confundir com o fork local, que executa scripts de terceiros selecionados pelo usuário.

## 3. Fork local compatível com Pocket Native

- Estado: **experimental**. Selecione uma pasta com `Cargo.toml`, `scripts/container/inside.sh`, `scripts/container/Containerfile` e `tools/title_id.py`.
- Use dump `.3ds`/`.cci` descriptografado; CIA está desativado nesse modo.
- A recompilação usa Docker/Podman e um volume de cache distinto. Os arquivos do fork são montados como somente leitura; o app **não baixa, altera nem atualiza** o fork.
- Verifique você mesmo a licença e a compatibilidade do fork com R36S.

## 4. Zakuro + 3dsrecomp (Windows)

- Estado: **adaptador externo experimental**; exige binário oficial compilado/instalado pelo próprio usuário.
- Baixe o Zakuro e o 3dsrecomp apenas do projeto original:
  - https://github.com/fearkov/zakuro
  - https://github.com/fearkov/3dsrecomp
- Instale as dependências indicadas no README dos autores, inclusive compilador C funcional.
- Na aba **Motores**, selecione o executável Windows do `3dsrecomp` (em geral `3dsrecomp.exe` ou `recomp3ds.exe`). O aplicativo invoca `3dsrecomp build ROM.3ds` sem usar um shell.
- Use `.3ds` ou `.cci` já descriptografado. As bibliotecas produzidas ficam onde o próprio 3dsrecomp/Zakuro determinarem. **Esse modo não cria um pacote para R36S** nem coloca jogo no destino escolhido.
- Não publique arquivos gerados contendo código de jogos comerciais.

## Outros projetos

- ScarletNX: pesquisa experimental de 3DS, sem gameplay declarado, licença GPLv3. Não integrado.
- Azahar: emulador de 3DS, GPLv3; não é recompilador estático. Não integrado.

Os créditos dos autores ficam na aba **Sobre / Apoiar** e em `THIRD_PARTY_NOTICES.md`. Não existe afiliação, aprovação ou parceria automática com qualquer mantenedor.

## Ainda precisa de teste real

Este pacote contém **código-fonte**. Execute `COMPILAR-UM-CLIQUE.cmd` no Windows, abra `dist\portable\ReNative3D36S.exe`, entre em **Motores** e confira as mensagens. Anexe o log caso haja erro. A seleção do executável externo deve ser feita somente com arquivos confiáveis, obtidos diretamente dos projetos oficiais.
