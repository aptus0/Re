$ErrorActionPreference = 'Stop'

$root = Join-Path $PSScriptRoot '..\force-app\main\default'

function Write-Utf8File([string]$RelativePath, [string]$Content) {
    $path = Join-Path $root $RelativePath
    $directory = Split-Path $path -Parent
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    [System.IO.File]::WriteAllText($path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Get-Option([hashtable]$Options, [string]$Name, $Default) {
    if ($Options.ContainsKey($Name)) { return $Options[$Name] }
    return $Default
}

function New-ObjectMetadata([string]$ApiName, [string]$Label, [string]$Plural, [string]$NameType = 'AutoNumber', [string]$DisplayFormat = '', [string]$SharingModel = 'ReadWrite') {
    $nameField = if ($NameType -eq 'AutoNumber') {
        "<displayFormat>$DisplayFormat</displayFormat><label>$Label No</label><type>AutoNumber</type>"
    } else {
        "<label>$Label Adı</label><type>Text</type>"
    }
    Write-Utf8File "objects\$ApiName\$ApiName.object-meta.xml" @"
<?xml version="1.0" encoding="UTF-8"?>
<CustomObject xmlns="http://soap.sforce.com/2006/04/metadata">
    <allowInChatterGroups>true</allowInChatterGroups>
    <deploymentStatus>Deployed</deploymentStatus>
    <enableActivities>true</enableActivities>
    <enableBulkApi>true</enableBulkApi>
    <enableFeeds>true</enableFeeds>
    <enableHistory>true</enableHistory>
    <enableReports>true</enableReports>
    <label>$Label</label>
    <nameField>$nameField</nameField>
    <pluralLabel>$Plural</pluralLabel>
    <sharingModel>$SharingModel</sharingModel>
</CustomObject>
"@
}

function New-Field([string]$Object, [string]$Name, [string]$Label, [string]$Type, [hashtable]$Options = @{}) {
    $extra = switch ($Type) {
        'Text' { "<length>$(Get-Option $Options 'Length' 255)</length>" }
        'LongTextArea' { "<length>$(Get-Option $Options 'Length' 32768)</length><visibleLines>$(Get-Option $Options 'Lines' 4)</visibleLines>" }
        'Number' { "<precision>$(Get-Option $Options 'Precision' 18)</precision><scale>$(Get-Option $Options 'Scale' 2)</scale>" }
        'Currency' { "<precision>$(Get-Option $Options 'Precision' 18)</precision><scale>$(Get-Option $Options 'Scale' 2)</scale>" }
        'Percent' { "<precision>$(Get-Option $Options 'Precision' 5)</precision><scale>$(Get-Option $Options 'Scale' 2)</scale>" }
        'Lookup' {
            "<deleteConstraint>SetNull</deleteConstraint><referenceTo>$($Options.ReferenceTo)</referenceTo><relationshipLabel>$($Options.RelationshipLabel)</relationshipLabel><relationshipName>$($Options.RelationshipName)</relationshipName>"
        }
        'MasterDetail' {
            "<referenceTo>$($Options.ReferenceTo)</referenceTo><relationshipLabel>$($Options.RelationshipLabel)</relationshipLabel><relationshipName>$($Options.RelationshipName)</relationshipName><reparentableMasterDetail>false</reparentableMasterDetail><writeRequiresMasterRead>false</writeRequiresMasterRead>"
        }
        'Picklist' {
            $values = ($Options.Values | ForEach-Object {
                $apiValue = if ($Options.ContainsKey('ApiValues') -and $Options.ApiValues.ContainsKey($_)) { $Options.ApiValues[$_] } else { $_ }
                "<value><fullName>$apiValue</fullName><default>false</default><label>$_</label></value>"
            }) -join ''
            "<valueSet><restricted>true</restricted><valueSetDefinition><sorted>false</sorted>$values</valueSetDefinition></valueSet>"
        }
        'Checkbox' { "<defaultValue>$(Get-Option $Options 'Default' 'false')</defaultValue>" }
        default { '' }
    }
    $required = if ($Options.Required -eq $true -and $Type -notin @('Lookup','MasterDetail','Checkbox')) { '<required>true</required>' } else { '' }
    $external = if ($Options.ExternalId -eq $true) { '<externalId>true</externalId><unique>true</unique>' } else { '' }
    Write-Utf8File "objects\$Object\fields\$Name.field-meta.xml" @"
<?xml version="1.0" encoding="UTF-8"?>
<CustomField xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>$Name</fullName>
    $external
    <label>$Label</label>
    $required
    <type>$Type</type>
    $extra
</CustomField>
"@
}

New-ObjectMetadata 'ERP_Warehouse__c' 'Depo' 'Depolar' 'Text'
New-Field 'ERP_Warehouse__c' 'External_Id__c' 'ERP Harici Kimlik' 'Text' @{ Length = 36; ExternalId = $true }
New-Field 'ERP_Warehouse__c' 'Code__c' 'Depo Kodu' 'Text' @{ Length = 50; Required = $true }
New-Field 'ERP_Warehouse__c' 'Description__c' 'Açıklama' 'LongTextArea' @{ Length = 4000; Lines = 3 }
New-Field 'ERP_Warehouse__c' 'Is_Default__c' 'Varsayılan Depo' 'Checkbox' @{ Default = 'false' }
New-Field 'ERP_Warehouse__c' 'Is_Active__c' 'Aktif' 'Checkbox' @{ Default = 'true' }

New-ObjectMetadata 'ERP_Stock_Item__c' 'Stok Kartı' 'Stok Kartları' 'Text'
New-Field 'ERP_Stock_Item__c' 'External_Id__c' 'ERP Harici Kimlik' 'Text' @{ Length = 36; ExternalId = $true }
New-Field 'ERP_Stock_Item__c' 'Product_Code__c' 'Ürün Kodu' 'Text' @{ Length = 80; Required = $true }
New-Field 'ERP_Stock_Item__c' 'Barcode__c' 'Barkod' 'Text' @{ Length = 80 }
New-Field 'ERP_Stock_Item__c' 'Warehouse__c' 'Depo' 'Lookup' @{ ReferenceTo = 'ERP_Warehouse__c'; RelationshipLabel = 'Stok Kartları'; RelationshipName = 'Stock_Items' }
New-Field 'ERP_Stock_Item__c' 'Current_Stock__c' 'Mevcut Stok' 'Number' @{ Precision = 18; Scale = 4 }
New-Field 'ERP_Stock_Item__c' 'Reserved_Stock__c' 'Rezerve Stok' 'Number' @{ Precision = 18; Scale = 4 }
New-Field 'ERP_Stock_Item__c' 'Minimum_Stock__c' 'Minimum Stok' 'Number' @{ Precision = 18; Scale = 4 }
New-Field 'ERP_Stock_Item__c' 'Maximum_Stock__c' 'Maksimum Stok' 'Number' @{ Precision = 18; Scale = 4 }
New-Field 'ERP_Stock_Item__c' 'Unit__c' 'Birim' 'Picklist' @{ Values = @('Adet','Kg','Lt','Metre','Kutu','Paket') }
New-Field 'ERP_Stock_Item__c' 'Unit_Cost__c' 'Birim Maliyet' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Stock_Item__c' 'Sale_Price__c' 'Satış Fiyatı' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Stock_Item__c' 'Vat_Rate__c' 'KDV Oranı' 'Percent' @{ Precision = 5; Scale = 2 }
New-Field 'ERP_Stock_Item__c' 'Track_Stock__c' 'Stok Takibi' 'Checkbox' @{ Default = 'true' }
New-Field 'ERP_Stock_Item__c' 'Allow_Negative__c' 'Negatif Stoğa İzin Ver' 'Checkbox' @{ Default = 'false' }
New-Field 'ERP_Stock_Item__c' 'Is_Active__c' 'Aktif' 'Checkbox' @{ Default = 'true' }
New-Field 'ERP_Stock_Item__c' 'Last_Sync_At__c' 'Son ERP Senkronizasyonu' 'DateTime'

New-ObjectMetadata 'ERP_Stock_Movement__c' 'Stok Hareketi' 'Stok Hareketleri' 'AutoNumber' 'SH-{000000}' 'ControlledByParent'
New-Field 'ERP_Stock_Movement__c' 'External_Id__c' 'ERP Harici Kimlik' 'Text' @{ Length = 36; ExternalId = $true }
New-Field 'ERP_Stock_Movement__c' 'Stock_Item__c' 'Stok Kartı' 'MasterDetail' @{ ReferenceTo = 'ERP_Stock_Item__c'; RelationshipLabel = 'Stok Hareketleri'; RelationshipName = 'Stock_Movements' }
New-Field 'ERP_Stock_Movement__c' 'Movement_Type__c' 'Hareket Türü' 'Picklist' @{ Values = @('Alış','Satış','Alış İade','Satış İade','Depo Transferi','Stok Sayımı','Fire / Zayiat','Üretim Girişi'); ApiValues = @{ 'Alış'='Alis'; 'Satış'='Satis'; 'Alış İade'='Alis_Iade'; 'Satış İade'='Satis_Iade'; 'Depo Transferi'='Depo_Transferi'; 'Stok Sayımı'='Stok_Sayimi'; 'Fire / Zayiat'='Fire_Zayiat'; 'Üretim Girişi'='Uretim_Girisi' } }
New-Field 'ERP_Stock_Movement__c' 'Direction__c' 'Yön' 'Picklist' @{ Values = @('Giriş','Çıkış'); ApiValues = @{ 'Giriş'='Giris'; 'Çıkış'='Cikis' } }
New-Field 'ERP_Stock_Movement__c' 'Quantity__c' 'Miktar' 'Number' @{ Precision = 18; Scale = 4; Required = $true }
New-Field 'ERP_Stock_Movement__c' 'Unit_Cost__c' 'Birim Maliyet' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Stock_Movement__c' 'Movement_Date__c' 'Hareket Tarihi' 'DateTime' @{ Required = $true }
New-Field 'ERP_Stock_Movement__c' 'Reference_Document__c' 'Referans Belge' 'Text' @{ Length = 100 }
New-Field 'ERP_Stock_Movement__c' 'Lot_Number__c' 'Lot Numarası' 'Text' @{ Length = 100 }
New-Field 'ERP_Stock_Movement__c' 'Serial_Number__c' 'Seri Numarası' 'Text' @{ Length = 100 }
New-Field 'ERP_Stock_Movement__c' 'Notes__c' 'Açıklama' 'LongTextArea' @{ Length = 4000; Lines = 3 }

New-ObjectMetadata 'ERP_Invoice__c' 'Fatura' 'Faturalar' 'AutoNumber' 'FTR-{000000}'
New-Field 'ERP_Invoice__c' 'External_Id__c' 'ERP Harici Kimlik' 'Text' @{ Length = 36; ExternalId = $true }
New-Field 'ERP_Invoice__c' 'Document_Number__c' 'Belge Numarası' 'Text' @{ Length = 80; Required = $true }
New-Field 'ERP_Invoice__c' 'Account__c' 'Müşteri' 'Lookup' @{ ReferenceTo = 'Account'; RelationshipLabel = 'ERP Faturaları'; RelationshipName = 'ERP_Invoices' }
New-Field 'ERP_Invoice__c' 'Warehouse__c' 'Depo' 'Lookup' @{ ReferenceTo = 'ERP_Warehouse__c'; RelationshipLabel = 'Faturalar'; RelationshipName = 'Invoices' }
New-Field 'ERP_Invoice__c' 'Status__c' 'Durum' 'Picklist' @{ Values = @('Taslak','Onay Bekliyor','Onaylandı','Kısmi Ödendi','Ödendi','İptal'); ApiValues = @{ 'Onay Bekliyor'='Onay_Bekliyor'; 'Onaylandı'='Onaylandi'; 'Kısmi Ödendi'='Kismi_Odendi'; 'Ödendi'='Odendi'; 'İptal'='Iptal' } }
New-Field 'ERP_Invoice__c' 'Document_Date__c' 'Fatura Tarihi' 'Date' @{ Required = $true }
New-Field 'ERP_Invoice__c' 'Due_Date__c' 'Vade Tarihi' 'Date'
New-Field 'ERP_Invoice__c' 'Sub_Total__c' 'Ara Toplam' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Discount_Amount__c' 'İndirim Tutarı' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Tax_Amount__c' 'KDV Tutarı' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Total_Amount__c' 'Toplam Tutar' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Paid_Amount__c' 'Tahsil Edilen' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Remaining_Amount__c' 'Kalan Bakiye' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice__c' 'Currency_Code__c' 'Para Birimi' 'Picklist' @{ Values = @('TRY','USD','EUR','GBP') }
New-Field 'ERP_Invoice__c' 'Exchange_Rate__c' 'Döviz Kuru' 'Number' @{ Precision = 18; Scale = 6 }
New-Field 'ERP_Invoice__c' 'Payment_Type__c' 'Ödeme Türü' 'Picklist' @{ Values = @('Nakit','Kredi Kartı','Havale / EFT','Vadeli') }
New-Field 'ERP_Invoice__c' 'E_Invoice_Status__c' 'e-Fatura Durumu' 'Picklist' @{ Values = @('Bekliyor','Gönderildi','Kabul Edildi','Reddedildi'); ApiValues = @{ 'Gönderildi'='Gonderildi'; 'Kabul Edildi'='Kabul_Edildi' } }
New-Field 'ERP_Invoice__c' 'E_Invoice_UUID__c' 'e-Fatura UUID' 'Text' @{ Length = 80 }
New-Field 'ERP_Invoice__c' 'Approval_Status__c' 'Onay Durumu' 'Picklist' @{ Values = @('Gerekli Değil','Bekliyor','Onaylandı','Reddedildi'); ApiValues = @{ 'Gerekli Değil'='Gerekli_Degil'; 'Onaylandı'='Onaylandi' } }
New-Field 'ERP_Invoice__c' 'Overdue_Days__c' 'Gecikme Günü' 'Number' @{ Precision = 6; Scale = 0 }
New-Field 'ERP_Invoice__c' 'Notes__c' 'Açıklama' 'LongTextArea' @{ Length = 8000; Lines = 4 }
New-Field 'ERP_Invoice__c' 'Last_Sync_At__c' 'Son ERP Senkronizasyonu' 'DateTime'

New-ObjectMetadata 'ERP_Invoice_Line__c' 'Fatura Kalemi' 'Fatura Kalemleri' 'AutoNumber' 'FK-{000000}' 'ControlledByParent'
New-Field 'ERP_Invoice_Line__c' 'Invoice__c' 'Fatura' 'MasterDetail' @{ ReferenceTo = 'ERP_Invoice__c'; RelationshipLabel = 'Fatura Kalemleri'; RelationshipName = 'Invoice_Lines' }
New-Field 'ERP_Invoice_Line__c' 'Stock_Item__c' 'Stok Kartı' 'Lookup' @{ ReferenceTo = 'ERP_Stock_Item__c'; RelationshipLabel = 'Fatura Kalemleri'; RelationshipName = 'Invoice_Lines' }
New-Field 'ERP_Invoice_Line__c' 'Product_Code__c' 'Ürün Kodu' 'Text' @{ Length = 80 }
New-Field 'ERP_Invoice_Line__c' 'Product_Name__c' 'Ürün / Hizmet' 'Text' @{ Length = 255; Required = $true }
New-Field 'ERP_Invoice_Line__c' 'Quantity__c' 'Miktar' 'Number' @{ Precision = 18; Scale = 4; Required = $true }
New-Field 'ERP_Invoice_Line__c' 'Unit__c' 'Birim' 'Picklist' @{ Values = @('Adet','Kg','Lt','Metre','Kutu','Paket','Gün') }
New-Field 'ERP_Invoice_Line__c' 'Unit_Price__c' 'Birim Fiyat' 'Currency' @{ Precision = 18; Scale = 2; Required = $true }
New-Field 'ERP_Invoice_Line__c' 'Discount_Percent__c' 'İndirim Oranı' 'Percent' @{ Precision = 5; Scale = 2 }
New-Field 'ERP_Invoice_Line__c' 'Vat_Rate__c' 'KDV Oranı' 'Percent' @{ Precision = 5; Scale = 2 }
New-Field 'ERP_Invoice_Line__c' 'Line_Total__c' 'Net Tutar' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice_Line__c' 'Tax_Amount__c' 'KDV Tutarı' 'Currency' @{ Precision = 18; Scale = 2 }
New-Field 'ERP_Invoice_Line__c' 'Sort_Order__c' 'Sıra' 'Number' @{ Precision = 5; Scale = 0 }

# Validation rules
Write-Utf8File 'objects\ERP_Stock_Movement__c\validationRules\Quantity_Must_Be_Positive.validationRule-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<ValidationRule xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>Quantity_Must_Be_Positive</fullName><active>true</active>
    <errorConditionFormula>Quantity__c &lt;= 0</errorConditionFormula>
    <errorDisplayField>Quantity__c</errorDisplayField><errorMessage>Miktar sıfırdan büyük olmalıdır.</errorMessage>
</ValidationRule>
'@
Write-Utf8File 'objects\ERP_Stock_Item__c\validationRules\Stock_Limits_Are_Valid.validationRule-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<ValidationRule xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>Stock_Limits_Are_Valid</fullName><active>true</active>
    <errorConditionFormula>AND(Maximum_Stock__c &gt; 0, Minimum_Stock__c &gt; Maximum_Stock__c)</errorConditionFormula>
    <errorMessage>Minimum stok, maksimum stoktan büyük olamaz.</errorMessage>
</ValidationRule>
'@
Write-Utf8File 'objects\ERP_Invoice__c\validationRules\Paid_Amount_Cannot_Exceed_Total.validationRule-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<ValidationRule xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>Paid_Amount_Cannot_Exceed_Total</fullName><active>true</active>
    <errorConditionFormula>Paid_Amount__c &gt; Total_Amount__c</errorConditionFormula>
    <errorDisplayField>Paid_Amount__c</errorDisplayField><errorMessage>Tahsil edilen tutar fatura toplamını aşamaz.</errorMessage>
</ValidationRule>
'@
Write-Utf8File 'objects\ERP_Invoice__c\validationRules\Due_Date_Not_Before_Document_Date.validationRule-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<ValidationRule xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>Due_Date_Not_Before_Document_Date</fullName><active>true</active>
    <errorConditionFormula>AND(NOT(ISBLANK(Due_Date__c)), Due_Date__c &lt; Document_Date__c)</errorConditionFormula>
    <errorDisplayField>Due_Date__c</errorDisplayField><errorMessage>Vade tarihi fatura tarihinden önce olamaz.</errorMessage>
</ValidationRule>
'@
Write-Utf8File 'objects\ERP_Invoice_Line__c\validationRules\Line_Amounts_Are_Valid.validationRule-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<ValidationRule xmlns="http://soap.sforce.com/2006/04/metadata">
    <fullName>Line_Amounts_Are_Valid</fullName><active>true</active>
    <errorConditionFormula>OR(Quantity__c &lt;= 0, Unit_Price__c &lt; 0, Discount_Percent__c &lt; 0, Discount_Percent__c &gt; 100)</errorConditionFormula>
    <errorMessage>Miktar pozitif, fiyat negatif olmayan ve indirim %0-%100 arasında olmalıdır.</errorMessage>
</ValidationRule>
'@

# Navigation
@(
    @{ Name='ERP_Warehouse__c'; Label='Depolar'; Icon='Custom55' },
    @{ Name='ERP_Stock_Item__c'; Label='Stok Kartları'; Icon='Custom37' },
    @{ Name='ERP_Stock_Movement__c'; Label='Stok Hareketleri'; Icon='Custom39' },
    @{ Name='ERP_Invoice__c'; Label='Faturalar'; Icon='Custom40' }
) | ForEach-Object {
    Write-Utf8File "tabs\$($_.Name).tab-meta.xml" "<?xml version=`"1.0`" encoding=`"UTF-8`"?>`n<CustomTab xmlns=`"http://soap.sforce.com/2006/04/metadata`"><customObject>true</customObject><motif>$($_.Icon): $($_.Label)</motif></CustomTab>"
}

Write-Utf8File 'applications\ReSoft_ERP.app-meta.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<CustomApplication xmlns="http://soap.sforce.com/2006/04/metadata">
    <brand><headerColor>#0176D3</headerColor><shouldOverrideOrgTheme>true</shouldOverrideOrgTheme></brand>
    <description>CRM, stok, depo ve fatura süreçleri için ReSoft ERP çalışma alanı.</description>
    <formFactors>Large</formFactors><formFactors>Small</formFactors>
    <label>ReSoft ERP</label>
    <navType>Standard</navType>
    <tabs>standard-Home</tabs><tabs>standard-Account</tabs><tabs>standard-Opportunity</tabs>
    <tabs>ERP_Stock_Item__c</tabs><tabs>ERP_Stock_Movement__c</tabs><tabs>ERP_Warehouse__c</tabs><tabs>ERP_Invoice__c</tabs>
    <uiType>Lightning</uiType>
</CustomApplication>
'@

$erpObjects = @('ERP_Warehouse__c','ERP_Stock_Item__c','ERP_Stock_Movement__c','ERP_Invoice__c','ERP_Invoice_Line__c')
$objectPermissions = ($erpObjects | ForEach-Object {
    "<objectPermissions><allowCreate>true</allowCreate><allowDelete>true</allowDelete><allowEdit>true</allowEdit><allowRead>true</allowRead><modifyAllRecords>false</modifyAllRecords><object>$_</object><viewAllRecords>false</viewAllRecords></objectPermissions>"
}) -join "`n"
$fieldPermissions = foreach ($object in $erpObjects) {
    Get-ChildItem (Join-Path $root "objects\$object\fields") -Filter '*.field-meta.xml' | ForEach-Object {
        $fieldName = $_.BaseName -replace '\.field-meta$', ''
        $fieldXml = Get-Content -Raw $_.FullName
        if ($fieldXml -notmatch '<required>true</required>' -and $fieldXml -notmatch '<type>MasterDetail</type>') {
            "<fieldPermissions><editable>true</editable><field>$object.$fieldName</field><readable>true</readable></fieldPermissions>"
        }
    }
}
$tabSettings = (@('ERP_Warehouse__c','ERP_Stock_Item__c','ERP_Stock_Movement__c','ERP_Invoice__c') | ForEach-Object {
    "<tabSettings><tab>$_</tab><visibility>Visible</visibility></tabSettings>"
}) -join "`n"
Write-Utf8File 'permissionsets\ReSoft_ERP_User.permissionset-meta.xml' @"
<?xml version="1.0" encoding="UTF-8"?>
<PermissionSet xmlns="http://soap.sforce.com/2006/04/metadata">
    <applicationVisibilities><application>ReSoft_ERP</application><visible>true</visible></applicationVisibilities>
    <classAccesses><apexClass>ErpOperationsController</apexClass><enabled>true</enabled></classAccesses>
    <description>Stok, depo, fatura ve fatura kalemi çalışma alanı kullanıcı yetkileri.</description>
    $($fieldPermissions -join "`n")
    <hasActivationRequired>false</hasActivationRequired><label>ReSoft ERP Kullanıcısı</label>
    $objectPermissions
    $tabSettings
</PermissionSet>
"@

Write-Host 'ERP Salesforce metadata generated.'
