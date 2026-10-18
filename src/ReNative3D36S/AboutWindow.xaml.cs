using System.Diagnostics;
using System.Windows;

namespace ReNative3D36S;

public partial class AboutWindow : Window
{
    private const string PixKey = "de8a6fb9-450d-4043-810f-0d2ceaf2e79c";
    // Código EMV BR Code Pix estático gerado para chave acima (sem valor fixo).
    private const string PixCode = "00020126580014br.gov.bcb.pix0136de8a6fb9-450d-4043-810f-0d2ceaf2e79c5204000053039865802BR5921OVERCLOCK INFORMATICA6007AIMORES62070503***6304DF07";
    private const string KofiUrl = "https://ko-fi.com/overclockinformatica";
    private const string InstagramUrl = "https://www.instagram.com/oc.informatica/";
    private const string LinksUrl = "https://linkme.bio/overclockinformatica";
    private const string PocketNativeUrl = "https://github.com/whovian64/pocket-native";

    public AboutWindow()
    {
        InitializeComponent();
        AboutProjectTitle.Text = LanguageService.Text("aboutProject");
        SupportTitle.Text = LanguageService.Text("support");
        SupportDescription.Text = LanguageService.Text("supportText");
        PixLabel.Text = LanguageService.Text("pix");
        KofiLabel.Text = LanguageService.Text("kofi");
        CopyPixBtn.Content = LanguageService.Text("copyPix");
        CopyKeyBtn.Content = LanguageService.Text("copyKey");
        OpenKofiBtn.Content = LanguageService.Text("openKofi");
        CopyKofiBtn.Content = LanguageService.Text("copyKofi");
        OriginalBtn.Content = LanguageService.Text("original");
        OnlineTitle.Text = LanguageService.Text("online");
        InstagramBtn.Content = LanguageService.Text("instagram");
        LinksBtn.Content = LanguageService.Text("links");
        CloseBtn.Content = LanguageService.Text("close");
        EngineCreditsTitle.Text = LanguageService.Text("engineCreditsTitle");
        CreditsPocketText.Text = LanguageService.Text("creditsPocket");
        CreditsZakuroText.Text = LanguageService.Text("creditsZakuro");
        CreditsOtherText.Text = LanguageService.Text("creditsOther");
        CreditsDisclaimerText.Text = LanguageService.Text("creditsDisclaimer");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void CopyPix_Click(object sender, RoutedEventArgs e) => CopyText(PixCode, "Código Pix copiado.");
    private void CopyPixKey_Click(object sender, RoutedEventArgs e) => CopyText(PixKey, "Chave Pix copiada.");
    private void CopyKofi_Click(object sender, RoutedEventArgs e) => CopyText(KofiUrl, "Link do Ko-fi copiado.");
    private void Kofi_Click(object sender, RoutedEventArgs e) => OpenUrl(KofiUrl);
    private void Instagram_Click(object sender, RoutedEventArgs e) => OpenUrl(InstagramUrl);
    private void Links_Click(object sender, RoutedEventArgs e) => OpenUrl(LinksUrl);
    private void PocketNative_Click(object sender, RoutedEventArgs e) => OpenUrl(PocketNativeUrl);
    private void Zakuro_Click(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/fearkov/zakuro");
    private void Recompiler_Click(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/fearkov/3dsrecomp");
    private void Scarlet_Click(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/scarletnx/ScarletNX");
    private void Azahar_Click(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/azahar-emu/azahar");

    private void CopyText(string content, string message)
    {
        try { Clipboard.SetText(content); MessageBox.Show(this, message, "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível copiar para a área de transferência: " + ex.Message); }
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível abrir o endereço: " + ex.Message); }
    }
}
