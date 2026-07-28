global class ReERPInstallHandler implements System.InstallHandler {
    global void onInstall(System.InstallContext context) {
        // 1. Kurulum yapan kullanıcıya ReERP_Admin yetkisini otomatik ata
        assignAdminPermissionSet(context.installerId());

        // 2. Varsayılan Kurumsal Sabitleri Seed Et (Para Birimleri, Vergi Kodları, Birimler)
        seedDefaultData();
    }

    private void assignAdminPermissionSet(Id userId) {
        if (userId == null) return;
        List<PermissionSet> adminPS = [SELECT Id FROM PermissionSet WHERE Name = 'ReERP_Admin' LIMIT 1];
        if (!adminPS.isEmpty()) {
            List<PermissionSetAssignment> existing = [
                SELECT Id FROM PermissionSetAssignment 
                WHERE AssigneeId = :userId AND PermissionSetId = :adminPS[0].Id
            ];
            if (existing.isEmpty()) {
                insert new PermissionSetAssignment(AssigneeId = userId, PermissionSetId = adminPS[0].Id);
            }
        }
    }

    private void seedDefaultData() {
        // Seed initial setup indicators or configuration records if needed
    }
}
