import { LightningElement, api, wire } from 'lwc';
import getInventorySummary from '@salesforce/apex/ErpOperationsController.getInventorySummary';

const CURRENCY = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
const NUMBER = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 4 });

export default class Stok360Dashboard extends LightningElement {
    @api recordId;
    data;
    error;

    @wire(getInventorySummary, { warehouseId: '$recordId' })
    wiredSummary({ data, error }) {
        if (data) {
            this.data = {
                ...data,
                totalValueLabel: CURRENCY.format(data.totalValue || 0),
                totalQuantityLabel: NUMBER.format(data.totalQuantity || 0),
                items: (data.items || []).map((item) => ({
                    ...item,
                    stockLabel: NUMBER.format(item.Current_Stock__c || 0),
                    availableLabel: NUMBER.format(
                        (item.Current_Stock__c || 0) - (item.Reserved_Stock__c || 0)
                    ),
                    valueLabel: CURRENCY.format(
                        (item.Current_Stock__c || 0) * (item.Unit_Cost__c || 0)
                    ),
                    stateClass:
                        item.Minimum_Stock__c != null &&
                        (item.Current_Stock__c || 0) <= item.Minimum_Stock__c
                            ? 'pill danger'
                            : 'pill success',
                    stateLabel:
                        item.Minimum_Stock__c != null &&
                        (item.Current_Stock__c || 0) <= item.Minimum_Stock__c
                            ? 'Kritik'
                            : 'Normal'
                })),
                movements: (data.movements || []).map((movement) => ({
                    ...movement,
                    quantityLabel: `${movement.Direction__c === 'Cikis' ? '-' : '+'}${NUMBER.format(
                        movement.Quantity__c || 0
                    )}`,
                    directionClass:
                        movement.Direction__c === 'Cikis' ? 'movement-out' : 'movement-in'
                }))
            };
            this.error = undefined;
        } else if (error) {
            this.error = error?.body?.message || 'Stok verileri yüklenemedi.';
            this.data = undefined;
        }
    }

    get hasItems() {
        return this.data?.items?.length > 0;
    }
}
