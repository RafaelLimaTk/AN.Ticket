using AN.Ticket.Application.DTOs.Contact;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Interfaces;
public interface IContactService
    : IService<ContactDto, Contact>
{
    Task<bool> CreateContactAsync(ContactCreateDto contactCreateDto);
    Task<bool> DeleteContactsAsync(List<Guid> ids);
    Task<ContactDetailsDto> GetContactDetailsAsync(Guid contactId);
    Task<Guid?> GetContactPaymentPlanIdAsync(Guid contactId);
    Task<PagedResult<ContactDto>> GetPaginatedContactsAsync(int pageNumber, int pageSize, string searchTerm = "");
    Task<bool> UpdateContactAsync(ContactCreateDto contactCreateDto);
    Task<(int Total, int Onhold)> GetTotalAndOnholdTicketsAsyn(string contactEmail);

}
