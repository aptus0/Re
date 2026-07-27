using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Desktop.Services;
using Re.Contracts.Auth;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace Re.Desktop.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly ISessionService _session;
    private bool _isPolling;

    public event EventHandler? LoginSucceeded;

    [ObservableProperty] private string _branchCode = string.Empty;
    [ObservableProperty] private string _storeCode = string.Empty;
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "123456";
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _loginButtonText = "API Bekleniyor...";
    
    // API Durum Yönetimi
    [ObservableProperty] private bool _isApiReady = false;
    [ObservableProperty] private string _apiStatusMessage = "API Sunucusu Başlatılıyor...";
    [ObservableProperty] private string _apiStatusColor = "#FF9900"; // Turuncu (Bekliyor)

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanLogin => IsApiReady && !IsLoading;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanLogin));
        if (IsApiReady)
        {
            LoginButtonText = value ? "Giriş yapılıyor..." : "Giriş Yap";
        }
    }

    partial void OnIsApiReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanLogin));
        if (value)
        {
            ApiStatusMessage = "Hazır";
            ApiStatusColor = "#4CAF50"; // Yeşil
            LoginButtonText = IsLoading ? "Giriş yapılıyor..." : "Giriş Yap";
        }
        else
        {
            ApiStatusMessage = "API Sunucusu Başlatılıyor...";
            ApiStatusColor = "#FF9900"; // Turuncu
            LoginButtonText = "API Bekleniyor...";
        }
    }

    public LoginViewModel(ApiClient apiClient, ISessionService session)
    {
        _apiClient = apiClient;
        _session = session;
        StartApiHealthCheck();
    }

    private void StartApiHealthCheck()
    {
        // 1. ADIM: Yerel API'yi otomatik başlatmayı dene
        try
        {
            Re.Desktop.Services.ApiRunnerService.StartApi();
        }
        catch
        {
            // Eğer zaten çalışıyorsa veya başlatılamadıysa hata fırlatma, polling işlemine devam et.
        }

        _isPolling = true;
        Task.Run(async () =>
        {
            while (_isPolling && !IsApiReady)
            {
                try
                {
                    // Sağlık kontrolü
                    var isHealthy = await _apiClient.CheckHealthAsync();
                    if (isHealthy)
                    {
                        // UI Thread'ine geçerek property'i güncelle (ObservableObject bunu handle edebilir ama güvende olalım)
                        App.Current.Dispatcher.Invoke(() => IsApiReady = true);
                    }
                }
                catch
                {
                    // Hata olursa (henüz kalkmadıysa) devam et
                }
                
                if (!IsApiReady)
                    await Task.Delay(2000); // 2 saniyede bir kontrol et
            }
        });
    }

    [RelayCommand]
    private void StartLocalApi()
    {
        if (!_isPolling)
        {
            StartApiHealthCheck();
        }
        else
        {
            // Zaten deniyorsa sadece servisi tekrar tetikle
            try { Re.Desktop.Services.ApiRunnerService.StartApi(); } catch { }
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!IsApiReady) return;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Kullanıcı adı boş olamaz.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Şifre boş olamaz.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var request = new LoginRequest(Username.Trim(), Password, BranchCode.Trim(), StoreCode.Trim());
            var response = await _apiClient.LoginAsync(request);

            if (response is not null)
            {
                _session.SetSession(response);
                _isPolling = false; // Döngüyü durdur
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "API sunucusuyla bağlantı koptu. Lütfen tekrar deneyin.";
            IsApiReady = false; // Tekrar beklemeye al
            StartApiHealthCheck();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Giriş hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        ErrorMessage = "Şifre sıfırlama için sistem yöneticinize başvurun.";
    }
}
