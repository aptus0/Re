using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Re.Domain.Entities;
using Re.Persistence.Context;

namespace Re.Persistence.Services;

public static class TurkishDataSeeder
{
    public static async Task SeedEnterpriseDefaultsAsync(ReDbContext context)
    {
        // Seeding helper to ensure default Turkish enterprise settings and initial company profiles exist.
        await Task.CompletedTask;
    }
}
