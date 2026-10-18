# ReNative3D36S v0.2.0 — revisão visual (dropdowns e Sobre)

## Correções

- `ComboBox` com template de aparência escura, incluindo superfície do menu aberto, foco e hover.
- Contraste garantido em todas as quatro listas: motor de compilação, motor na aba Motores, idioma da lateral e idioma em Configurações.
- Itens selecionados destacados em azul e texto claro. O menu suspenso não depende das cores do tema claro do Windows.
- Página Sobre/Apoiar reorganizada: **links e QR codes das redes**; **QR Pix/Ko-fi**; **informações do projeto**; **créditos dos motores**.
- Preservados caminhos, botões, funções de cópia e links dos QR codes, créditos e avisos de licença existentes.

## Como publicar no seu PC

1. Extraia este pacote em uma pasta limpa; mantenha `.tools` da versão anterior, se desejar, apenas como SDK local.
2. Execute `COMPILAR-UM-CLIQUE.cmd`.
3. Abra `dist\portable\ReNative3D36S.exe` e teste ambos os seletores de motor e ambos os seletores de idioma.
4. Abra `Sobre / Apoiar` e confirme os quatro QR codes acima dos créditos.

**Nota:** a interface WPF só pode ser validada visualmente após compilar e executar no Windows. Os testes realizados neste ambiente foram estáticos.
