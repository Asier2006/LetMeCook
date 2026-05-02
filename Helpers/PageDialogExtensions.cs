// Métodos auxiliares para centralizar llamadas asíncronas a diálogos de MAUI.

namespace MiniTFG;

    public static class PageDialogExtensions
{
    public static Task DisplayAlertAsync(this Page page, string title, string message, string cancel)
    {
        return page.DisplayAlert(title, message, cancel);
    }

    public static Task<bool> DisplayAlertAsync(this Page page, string title, string message, string accept, string cancel)
    {
        return page.DisplayAlert(title, message, accept, cancel);
    }

    public static Task<string> DisplayActionSheetAsync(this Page page, string title, string cancel, string destruction, params string[] buttons)
    {
        return page.DisplayActionSheet(title, cancel, destruction, buttons);
    }
}
