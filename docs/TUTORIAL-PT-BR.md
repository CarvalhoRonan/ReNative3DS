# ReNative3D36S v0.2.0-beta.1: tutorial passo a passo

> Versão experimental para Windows 10/11 x64. Este tutorial explica tanto a criação do EXE do ReNative3D36S quanto a recompilação de um jogo **para o R36S**. O EXE Windows não executa jogos 3DS no PC.

## 1. Antes de começar

Você precisa de Windows x64, internet na primeira configuração, espaço livre em disco (vários GB para imagens/compilação), virtualização habilitada, Docker Desktop/Podman e um R36S com ArkOS ou sistema compatível. Use ROMs extraídas de jogos de sua propriedade; os arquivos `.3ds`/`.cci` precisam estar descriptografados. Arquivos `.cia` podem precisar do `aes_keys.txt` do **seu próprio** console.

Não fornecemos ROMs, chaves AES ou firmware. Não os envie ao GitHub nem anexos de issues.

## 2. Baixar o código-fonte

1. Acesse [CarvalhoRonan/ReNative3DS](https://github.com/CarvalhoRonan/ReNative3DS).
2. Clique em **Code → Download ZIP** e extraia os arquivos.
3. Coloque o projeto preferencialmente em `C:\Projetos\ReNative3D36S`. Evite OneDrive ou pastas sincronizadas para prevenir bloqueios durante a compilação.
4. Abra a pasta em que está `COMPILAR-UM-CLIQUE.cmd`.

## 3. Gerar ReNative3D36S.exe

1. Dê **dois cliques** em `COMPILAR-UM-CLIQUE.cmd`.
2. O script verifica a última release estável do Pocket Native e procura o SDK .NET 10. Se necessário, baixa o SDK para `.tools\dotnet`, sem precisar instalá-lo globalmente.
3. Aguarde restauração dos pacotes NuGet e publicação WPF em modo self-contained.
4. Se for concluído, o arquivo estará em `dist\portable\ReNative3D36S.exe`. Ele não exige o runtime .NET no PC de destino.
5. Se houver falha, consulte `logs\publicacao-*.log` e `logs\processo-*.out.log` / `.err.log`.

A compilação do **aplicativo Windows** não é a recompilação de um jogo: para gerar arquivos ARM64 para o R36S, o Docker/Podman continuará obrigatório.

## 4. Docker Desktop: obrigatório para recompilar ROMs

**Abra o Docker Desktop antes de clicar em Iniciar recompilação.** Aguarde a inicialização completa até o Docker indicar que o motor está em execução (engine running). Abrir apenas o ícone não é suficiente.

- Se for a primeira instalação, aceite a configuração de WSL 2/virtualização recomendada pelo Docker. Windows pode pedir elevação e reinicialização. Depois da reinicialização, abra o Docker Desktop novamente.
- **Quando o Docker solicitar permissão de compartilhamento de unidade ou pasta, permita o acesso à pasta da ROM e à pasta de destino.** Exemplo: arquivos de entrada e saída em `E:\Videogames\` precisam ser visíveis para o contêiner.
- Em algumas instalações, a opção fica em **Docker Desktop → Settings → Resources → File Sharing**. Adicione as unidades/diretórios relevantes, aplique as alterações e reinicie o serviço. A tela varia com a versão e pode não existir no backend WSL 2; nesse caso, o compartilhamento costuma ser automático, mas as permissões do Windows/WSL e o funcionamento do motor continuam necessários.
- Caso veja `Mounts denied`, `drive is not shared`, `permission denied`, `invalid mount`, ou erro de bind mount, revise as permissões e teste novamente. Feche aplicativos que prendam os arquivos da ROM, especialmente sincronizadores.
- **Nunca envie seu arquivo de chaves para a internet** para tentar resolver falhas de acesso.

Referência de configurações: [Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/).

## 5. Recompilar um jogo

1. Abra `ReNative3D36S.exe`.
2. Em **Idioma**, escolha automático ou English, Português, Español, Русский.
3. Clique em **Escolher ROM** e selecione seu arquivo `.3ds`, `.cci` ou `.cia`.
4. Para `.cia`, quando solicitado, selecione o `aes_keys.txt` extraído do seu console. A GUI verifica a presença de algumas entradas; isso não garante compatibilidade com todos os arquivos CIA.
5. Em **Escolher pasta**, informe o diretório pai em que as pastas dos jogos serão criadas.
6. Deixe marcada **Criar pasta com nome do jogo**. O nome padrão vem do perfil da compilação, quando disponível. Se desejar, escreva outro nome no campo opcional.
7. Confirme que **Docker Desktop está aberto, com o motor ativo e com acesso às pastas**. Clique **Iniciar recompilação**.
8. A interface verifica a versão estável do Pocket Native, prepara o contêiner, executa os scripts oficiais e registra os resultados. A barra mostra marcos estimados, não tempo restante exato.
9. Quando a compilação terminar, use **Abrir resultado**.

A primeira execução pode demorar bastante para preparar Rust, Ubuntu e ferramentas ARM64. Compilações seguintes reaproveitam o cache. Nem todos os títulos 3DS funcionam no Pocket Native; o relatório de compatibilidade do projeto original é a referência.

## 6. Copiar para o R36S

A pasta de saída escolhida conterá **um diretório com o nome do jogo e um arquivo `.sh` ao lado**. Por exemplo:

```text
Recomps/
  Pokémon Omega Ruby/
    pocket-native/
      pocket-native
      run.sh
      games/<title-id>/title.so
      games/<title-id>/game.3ds
  Pokémon Omega Ruby.sh
```

1. Insira o cartão SD do portátil no computador.
2. Encontre a partição de jogos e abra `roms\ports` (ou `roms2\ports` se usar dois cartões).
3. Copie **os dois itens, a pasta `Pokémon Omega Ruby` E o `Pokémon Omega Ruby.sh`, juntos**, para `ports`.
4. Não mova o launcher para dentro da pasta, nem renomeie apenas um dos itens: o arquivo `.sh` referencia o nome exato da pasta.
5. Ejete o cartão com segurança, coloque-o no R36S e reinicie o EmulationStation. O jogo deverá aparecer em **Ports** se a instalação/firmware forem compatíveis.

> O processo original do Pocket Native usa uma pasta compartilhada `Pocket Native`. O ReNative cria pacotes individuais e um launcher de nível superior para facilitar a instalação. Para sistemas com comportamento diferente, consulte as instruções oficiais do autor.

## 7. Atualizações independentes do executável

- **Verificar atualizações** consulta a última release estável do Pocket Native no GitHub.
- **Atualizar motor** substitui somente os arquivos do motor armazenados no PC, guardando o núcleo anterior. Não modifica nem recompila o `ReNative3D36S.exe`.
- **Atualizar perfis** baixa a pasta oficial `titles/` e guarda uma cópia da anterior. Não modifica jogos já gerados.
- Antes de cada recompilação, o aplicativo tenta atualizar o motor estável. Se estiver offline, usa a cópia local quando disponível.

**Atenção:** jogos recompilados antes de atualizar o motor podem precisar de nova recompilação se o motor ou overrides de perfis forem incompatíveis. Atualizar perfis no PC não altera os binários já copiados ao cartão SD.

## 8. Erros comuns e como identificar

- **Docker não inicia:** abra Docker Desktop, aguarde o motor, finalize WSL 2/virtualização/reinicialização.
- **Docker não acessa ROM ou destino:** permita a pasta/unidade nos ajustes de compartilhamento quando apresentados; confira permissões, WSL2 e caminho de entrada/saída.
- **Arquivo CIA falha:** leia o log de `cia2cci.py`. Arquivos com CIA-layer titlekey encrypted ou seeded NCCH podem não ser compatíveis mesmo com `aes_keys.txt`. Para casos não suportados, use `.3ds/.cci` descriptografado de seu próprio jogo.
- **Código de saída 1:** não é diagnóstico. Abra `logs` pelo botão da GUI e leia a mensagem emitida antes do encerramento.
- **Jogo não aparece em Ports:** confirme o `.sh` no nível superior de `roms/ports`, ao lado da pasta com o mesmo nome. Reinicie EmulationStation.
- **Executável abre mas não recompila:** o EXE ser standalone não dispensa o Docker e suas permissões.
- **Abertura ou GPU falha no R36S:** valide ArkOS/firmware e drivers compatíveis e consulte os logs no cartão SD.

## 9. Instruções originais do Pocket Native

**Se encontrar problemas ou erros, além deste tutorial, siga também as instruções e recomendações do projeto original.** A GUI organiza os comandos, mas a compatibilidade final é determinada pelo motor mantido pelos autores upstream.

- [Repositório e README original](https://github.com/whovian64/pocket-native)
- [START_HERE.md: instalação, requisitos e resolução de problemas](https://github.com/whovian64/pocket-native/blob/master/START_HERE.md)
- [docs/games.md: perfis, jogos e limitações](https://github.com/whovian64/pocket-native/blob/master/docs/games.md)
- [Issues do Pocket Native](https://github.com/whovian64/pocket-native/issues)

Os comandos oficiais de compilação, como `scripts/build.ps1`, ainda são referências úteis para comparar resultados com a GUI. Antes de reportar um problema, remova chaves, ROMs, nomes privados e dados pessoais dos logs.

## 10. Créditos e apoio

ReNative3D36S: Overclock Informática. Motor: Pocket Native por whovian64, derivado de Zakuro e 3dsrecomp de fearkov. Licença do frontend: MIT, sem substituir outras licenças dos componentes. [Ko-fi](https://ko-fi.com/overclockinformatica), [Instagram](https://www.instagram.com/oc.informatica/) e [links da Overclock Informática](https://linkme.bio/overclockinformatica). Apoio voluntário.


## Interface v0.1.9: onde encontrar cada função

- **Recompilar:** campos para escolher a ROM e o destino, prévia da pasta do jogo, preparação do Docker, progresso e registro de atividades.
- **Atualizações:** verificar a versão estável mais recente do motor Pocket Native, instalar a atualização e atualizar apenas os perfis.
- **Biblioteca:** localizar pastas de jogos previamente recompilados dentro do destino selecionado e abri-las no Explorador de Arquivos.
- **Configurações:** alterar manualmente o idioma ou manter detecção automática. Há links para as instruções oficiais do Pocket Native e do Docker.
- **Sobre / Apoiar:** informações de créditos e suporte voluntário por Pix ou Ko-fi.

O **banner superior não possui ilustrações dos consoles**; a identidade R3D é tipográfica. Os atalhos à direita permitem abrir a pasta de saída, o cache do Pocket Native e os logs. A opção de limpar logs limpa somente o painel na tela: o arquivo local de diagnóstico é preservado.

**Antes de testar a recompilação:** abra o **Docker Desktop**, espere a mensagem de motor pronto e, se solicitadas, conceda permissões de compartilhamento para a pasta da ROM e a pasta de destino. Em casos de problemas no Linux interno, permissões do volume ou incompatibilidade do jogo, **consulte também o guia original**: https://github.com/whovian64/pocket-native/blob/master/START_HERE.md e https://github.com/whovian64/pocket-native/blob/master/docs/games.md.


## Revisão da interface v0.2.0: menus e tela Sobre

Os dropdowns **Motor selecionado** (Recompilar e Motores) e **Idioma** (Configurações e lateral) utilizam fundo escuro tanto fechados quanto abertos, com texto branco e seleção azul. Para testar: abra cada um, mova o mouse sobre as opções e altere a seleção.

Na tela **Sobre / Apoiar**, as redes sociais e seus QR codes aparecem primeiro; em seguida Pix e Ko-fi; depois a apresentação do projeto e os créditos/licenças dos autores dos motores.
