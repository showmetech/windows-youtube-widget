# YouTube Widget para Windows

Player flutuante com recomendações da conta conectada no YouTube pelo Chrome, transferência no ponto atual e controles de reprodução. Fica na bandeja, com atalho na área de trabalho e inicialização opcional com o Windows.

**[Baixar a versão mais recente](https://github.com/showmetech/windows-youtube-widget/releases/latest)** · [Privacidade](https://showmetech.github.io/windows-youtube-widget/privacy.html) · [Suporte](https://github.com/showmetech/windows-youtube-widget/issues)

## Compatibilidade

- Windows 10 build 19041 ou posterior e Windows 11: x64, x86 e ARM64.
- .NET e Microsoft WebView2 incluídos nos instaladores/arquivos portáteis, sem instalação separada desses componentes.
- Internet e acesso ao YouTube são necessários. Recomendações e transferência exigem a extensão e login no próprio navegador.
- Restrições de idade/região, vídeos privados, incorporação bloqueada e políticas corporativas podem impedir recursos específicos.
- Windows S mode, ARM32, Xbox, macOS e Linux não são suportados. Não há promessa de funcionar em todo hardware ou reproduzir todos os vídeos.

As três arquiteturas são compiladas no CI. Interface e reprodução foram testadas em Windows x64. Testes físicos em ARM64/Windows 10 não são presumidos a partir de compilação.

## Uso

Escolha o instalador para a arquitetura do Windows. Fechar pausa o vídeo e recolhe o widget para a bandeja. Anterior/próximo trocam o vídeo; o botão de tamanho alterna os modos ampliado e compacto. Configurações incluem temas, fila, favoritos, startup e conexão.

Veja [Conectar o YouTube](docs/CONECTAR-YOUTUBE.md). A extensão requer ativação manual enquanto sua publicação na Chrome Web Store não estiver concluída. O ZIP de desenvolvimento mantém sua chave pública para conservar o ID local; o ZIP para a loja exclui essa chave. Senhas, cookies e sessões não são incluídos.

## Microsoft Store e atualizações

O cadastro existente **Showmetech / SHOWMETECH LTDA** está em verificação pela Microsoft. A listagem ainda não está publicada na Microsoft Store. Cada atualização deve seguir [RELEASING.md](docs/RELEASING.md): GitHub e recibo da Store correspondentes à mesma versão. A automação envia o MSIX após habilitar o produto e credenciais existentes; a Microsoft determina o fim da certificação.

Código em src, extensão em extension, iniciador em startup e rotinas em scripts. VERSION controla a versão do app. Veja [o preparo da Store](docs/STORE-ONBOARDING.md).

Produto independente de Bruno Ayres Martinez / Showmetech, sem vínculo oficial com Google, YouTube ou Microsoft. Usa o player oficial; não extrai streams, baixa vídeos ou remove anúncios/marcas do YouTube. Componentes de terceiros mantêm suas licenças. A publicação do código não concede uma licença de software livre não definida pelo titular.
