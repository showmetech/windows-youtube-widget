# Conectar o YouTube ao widget

1. Instale o YouTube Widget para a arquitetura do seu Windows e abra-o uma vez. O aplicativo registra a conexão nativa do Chrome/Edge no usuário atual, sem copiar contas ou cookies.
2. Enquanto a extensão ainda não estiver publicada na Chrome Web Store, abra chrome://extensions no Chrome, habilite o modo de desenvolvedor e use Carregar sem compactação. Selecione a pasta extension do aplicativo instalado: %LOCALAPPDATA%\Programs\WindowsYouTubeWidget\extension. Em uma distribuição portátil, selecione a pasta extension dentro do diretório extraído e abra primeiro WindowsYouTubeWidget.exe nesse diretório.
3. No popup YouTube Widget, habilite Conectar recomendações ao widget e abra a página inicial do YouTube na conta desejada. Uma conta conectada em outro perfil de Chrome não é transferida para este perfil.
4. Ao abrir o widget pela bandeja, ele pede uma página inicial temporária em segundo plano e usa a nova lista recomendada. A extensão lê o conteúdo público da página e seus dados iniciais; ela não consulta endpoints privados. A aba criada é fechada após leitura/expiração se você não a tiver selecionado.
5. Na página de vídeo, o botão com quatro painéis transfere o vídeo e a posição para o widget. A página pausa antes de o widget confirmar a continuidade. Se você retomar explicitamente no navegador, o widget pausa antes de a página continuar.

Se recomendações não forem desejadas, desative a opção no popup. A transferência por clique continua disponível. No widget também é possível abrir links manualmente nas configurações.

Após uma atualização do aplicativo, reinicie o widget para atualizar o caminho da conexão nativa. Após uma mudança real no código da extensão de desenvolvimento, recarregue-a no gerenciador e atualize a aba do YouTube. Não é necessário fazer isso por uma atualização apenas do empacotamento do desktop.

As permissões corporativas e do navegador continuam valendo. Não há instalação forçada nem desativação de proteções. Conteúdo privado, com restrições ou sem autorização de incorporação pode precisar ser assistido no próprio site. O aplicativo é independente e não oferece todos os recursos do YouTube Premium.
