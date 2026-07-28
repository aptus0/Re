import { LightningElement, track } from 'lwc';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';

export default class ReerpSetupWizard extends LightningElement {
    @track companyName = 'Re ERP Demo A.Ş.';
    @track taxNumber = '1234567890';
    @track taxOffice = 'Büyük Mükellefler V.D.';
    @track currencyCode = 'TRY';

    currencyOptions = [
        { label: 'TRY - Türk Lirası (₺)', value: 'TRY' },
        { label: 'USD - US Dollar ($)', value: 'USD' },
        { label: 'EUR - Euro (€)', value: 'EUR' }
    ];

    handleCompanyChange(event) { this.companyName = event.target.value; }
    handleTaxChange(event) { this.taxNumber = event.target.value; }
    handleTaxOfficeChange(event) { this.taxOffice = event.target.value; }
    handleCurrencyChange(event) { this.currencyCode = event.target.value; }

    handleCompleteSetup() {
        this.dispatchEvent(new ShowToastEvent({
            title: 'Başarılı!',
            message: 'Re ERP kurulumu tamamlandı ve varsayılan veriler aktif edildi.',
            variant: 'success'
        }));
    }
}
