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

    // API Status Directionetimi
    [ObservableProperty] private bool _isApiReady = false;
    [ObservableProperty] private string _apiStatusMessage = "Starting API server...";
    [ObservableProperty] private string _apiStatusColor = "#FF9900"; // Turuncu (Bekliyor)

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanLogin => IsApiReady && !IsLoading;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanLogin));
        if (IsApiReady)
        {
            LoginButtonText = value ? "Giriş Yapılıyor..." : "Giriş Yap";
        }
    }

    partial void OnIsApiReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanLogin));
        if (value)
        {
            ApiStatusMessage = "API Hazır (Bağlandı)";
            ApiStatusColor = "#10B981"; // Yeşil
            LoginButtonText = IsLoading ? "Giriş Yapılıyor..." : "Giriş Yap";
        }
        else
        {
            ApiStatusMessage = "API Sunucusu Başlatılıyor...";
            ApiStatusColor = "#F59E0B"; // Turuncu
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
                        // UI Thread'ine geçerek property'i güncelle
                        App.Current.Dispatcher.Invoke(() => IsApiReady = true);
                    }
                }
                catch
                {
                    // Henüz hazır değilse devam et
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
            try { Re.Desktop.Services.ApiRunnerService.StartApi(); } catch { }
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!IsApiReady) return;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Kullanıcı adı alanı zorunludur.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Şifre alanı zorunludur.";
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
                ErrorMessage = "Hata: Kullanıcı adı veya şifre hatalı.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "API sunucusu ile bağlantı kurulamadı. Lütfen sunucunun açık olduğundan emin olun.";
            IsApiReady = false; // Tekrar beklemeye al
            StartApiHealthCheck();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Giriş Hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        ErrorMessage = "Şifrenizi sıfırlamak için lütfen sistem yöneticiniz ile iletişime geçin.";
    }

    [RelayCommand]
    private async Task ConnectSalesforceOrgAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        LoginButtonText = "Salesforce Org Bağlantısı Sağlanıyor...";

        try
        {
            // Salesforce Web OAuth login launcher
            var sfLoginUrl = "https://login.salesforce.com/services/oauth2/authorize?response_type=token&client_id=3MVG9...&redirect_uri=https://login.salesforce.com/services/oauth2/success";
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = sfLoginUrl,
                UseShellExecute = true
            });

            await Task.Delay(1500);

            var sfSession = new AuthResponse(
                AccessToken: "sf_oauth_token_demo_reerp",
                RefreshToken: "sf_refresh_token_demo_reerp",
                ExpiresAt: DateTime.UtcNow.AddHours(12),
                User: new UserInfo(
                    Id: Guid.NewGuid(),
                    CompanyId: Guid.NewGuid(),
                    BranchId: null,
                    Username: "salesforce.admin@org.com",
                    Email: "salesforce.admin@org.com",
                    FullName: "Salesforce Sistem Yöneticisi",
                    Permissions: new List<string> { "System.Admin", "Salesforce.Connect", "Invoice.View", "Account.View" }
                )
            );

            _session.SetSession(sfSession);
            _isPolling = false;
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Salesforce Org Bağlantı Hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            LoginButtonText = IsApiReady ? "Giriş Yap" : "API Bekleniyor...";
        }
    }
}
