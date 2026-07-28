using System;
using System.Windows;
using Re.Desktop.Services;

namespace Re.Desktop.Views.Finance;

public partial class ChequeDetailWindow : Window
{
    public ChequeDetailWindow(ChequeNoteItem item)
    {
        InitializeComponent();
        DataContext = new ChequeDetailViewModel(item);
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class ChequeDetailViewModel
{
    public ChequeNoteItem Item { get; }
    public string AmountInWords { get; }
    public bool HasFinalStatus => Item.Status != ChequeNoteStatus.Portfolio;

    public ChequeDetailViewModel(ChequeNoteItem item)
    {
        Item = item;
        AmountInWords = ConvertToWords(item.Amount);
    }

    private static string ConvertToWords(decimal amount)
    {
        try
        {
            long lira = (long)Math.Floor(amount);
            int kurus = (int)Math.Round((amount - lira) * 100);

            string liraStr = NumberToWords(lira) + " Türk Lirası";
            string kurusStr = kurus > 0 ? " " + NumberToWords(kurus) + " Kuruş" : "";

            return "Yalnız " + liraStr + kurusStr;
        }
        catch
        {
            return $"{amount:N2} TL";
        }
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Sıfır";

        string[] units = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
        string[] tens = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Atmış", "Yetmiş", "Seksen", "Doksan" };
        string[] thousands = { "", "Bin", "Milyon", "Milyar", "Trilyon" };

        string words = "";
        int i = 0;

        while (number > 0)
        {
            long part = number % 1000;
            if (part > 0)
            {
                string partStr = "";
                int hundreds = (int)(part / 100);
                int remainder = (int)(part % 100);
                int tenDigit = remainder / 10;
                int unitDigit = remainder % 10;

                if (hundreds > 0)
                {
                    if (hundreds > 1) partStr += units[hundreds];
                    partStr += "Yüz";
                }

                partStr += tens[tenDigit];
                partStr += units[unitDigit];

                if (i == 1 && partStr == "Bir") partStr = ""; // Bin, not Bir Bin

                words = partStr + thousands[i] + words;
            }
            number /= 1000;
            i++;
        }

        return words;
    }
}
