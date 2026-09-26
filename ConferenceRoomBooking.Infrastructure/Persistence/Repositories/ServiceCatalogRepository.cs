using ConferenceRoomBooking.Application.DTOs.Services;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Repositories;

public sealed class ServiceCatalogRepository(AppDbContext dbContext) : IServiceCatalog
{
    public async Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Services
            .AsNoTracking()
            .Where(service => service.IsActive)
            .OrderBy(service => service.Id)
            .Select(service => new ServiceResponse(service.Id, service.Name, service.Price))
            .ToListAsync(cancellationToken);
    }
}
