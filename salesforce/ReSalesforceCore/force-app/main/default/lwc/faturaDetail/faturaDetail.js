import { LightningElement, api, wire } from 'lwc';
import getInvoiceWorkspace from '@salesforce/apex/ErpOperationsController.getInvoiceWorkspace';

const money = (value, currency = 'TRY') =>
    new Intl.NumberFormat('tr-TR', { style: 'currency', currency: currency || 'TRY' }).format(
        value || 0
    );

export default class FaturaDetail extends LightningElement {
    @api recordId;
    workspace;
    error;

    @wire(getInvoiceWorkspace, { invoiceId: '$recordId' })
    wiredInvoice({ data, error }) {
        if (data) {
            const invoice = data.invoice;
            const currency = invoice.Currency_Code__c || 'TRY';
            this.workspace = {
                ...data,
                invoice: {
                    ...invoice,
                    subTotalLabel: money(invoice.Sub_Total__c, currency),
                    discountLabel: money(invoice.Discount_Amount__c, currency),
                    taxLabel: money(invoice.Tax_Amount__c, currency),
                    totalLabel: money(invoice.Total_Amount__c, currency),
                    paidLabel: money(invoice.Paid_Amount__c, currency),
                    remainingLabel: money(invoice.Remaining_Amount__c, currency),
                    statusClass: `status status-${this.statusTone(invoice.Status__c)}`
                },
                lines: (data.lines || []).map((line, index) => ({
                    ...line,
                    rowNumber: index + 1,
                    unitPriceLabel: money(line.Unit_Price__c, currency),
                    lineTotalLabel: money(line.Line_Total__c, currency)
                })),
                dueLabel:
                    data.daysToDue == null
                        ? 'Vade yok'
                        : data.daysToDue >= 0
                          ? `${data.daysToDue} gün kaldı`
                          : `${Math.abs(data.daysToDue)} gün gecikti`,
                progressStyle: `width:${Math.min(100, Math.max(0, data.collectionRate || 0))}%`
            };
            this.error = undefined;
        } else if (error) {
            this.error = error?.body?.message || 'Fatura verileri yüklenemedi.';
            this.workspace = undefined;
        }
    }

    statusTone(status) {
        if (status === 'Odendi' || status === 'Onaylandi') return 'success';
        if (status === 'Iptal') return 'danger';
        return 'warning';
    }
}
