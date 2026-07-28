import { LightningElement, track } from 'lwc';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';

export default class ReerpInvoiceEditor extends LightningElement {
    @track invoiceDirection = 'SALES';
    @track invoiceDate = new Date().toISOString().substring(0, 10);
    @track lines = [
        { id: 1, description: 'Yazılım Danışmanlık Hizmeti', quantity: 1, unitPrice: 10000, vatRate: 20, total: 12000 }
    ];

    directionOptions = [
        { label: 'Satış Faturası (Çıkış)', value: 'SALES' },
        { label: 'Alış Faturası (Giriş)', value: 'PURCHASE' }
    ];

    handleDirectionChange(event) { this.invoiceDirection = event.target.value; }

    handleAddLine() {
        const nextId = this.lines.length + 1;
        this.lines = [...this.lines, { id: nextId, description: 'Yeni Ürün / Hizmet', quantity: 1, unitPrice: 1000, vatRate: 20, total: 1200 }];
    }

    handlePostInvoice() {
        this.dispatchEvent(new ShowToastEvent({
            title: 'Başarılı!',
            message: 'Fatura onaylandı ve muhasebe/stok kayıtları post edildi.',
            variant: 'success'
        }));
    }
}
