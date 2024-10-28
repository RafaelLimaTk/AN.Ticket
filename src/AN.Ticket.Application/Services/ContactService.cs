using AN.Ticket.Application.DTOs.Contact;
using AN.Ticket.Application.Extensions;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;
using AN.Ticket.Domain.ValueObjects;

namespace AN.Ticket.Application.Services;
public class ContactService
    : Service<ContactDto, Contact>, IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentPlanRepository _paymentPlanRepository;

    public ContactService(
        IRepository<Contact> repository,
        IContactRepository contactRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IPaymentPlanRepository paymentPlanRepository
    )
        : base(repository)
    {
        _contactRepository = contactRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _paymentPlanRepository = paymentPlanRepository;

    }

    public async Task<bool> CreateContactAsync(
        ContactCreateDto contactCreateDto
    )
    {
        if (!CpfValidator.Validate(contactCreateDto.Cpf))
            throw new EntityValidationException("Cpf inválido.");

        var existingContact = await _contactRepository.ExistContactCpfAsync(contactCreateDto.Cpf);
        if (existingContact)
            throw new EntityValidationException("Contato já existe com esse cpf.");

        var contact = new Contact(
            contactCreateDto.FirstName!,
            contactCreateDto.LastName!,
            contactCreateDto.PrimaryEmail!,
            contactCreateDto.SecondaryEmail!,
            contactCreateDto.Phone!,
            contactCreateDto.Mobile!,
            contactCreateDto.Department!,
            contactCreateDto.Title!
        );

        contact.ChangedCpf(contactCreateDto.Cpf);

        if (contactCreateDto.SocialNetworks != null)
        {
            foreach (var socialNetworkDto in contactCreateDto.SocialNetworks)
            {
                contact.AddSocialNetwork(new SocialNetwork(
                    socialNetworkDto.Name!,
                    socialNetworkDto.Url!,
                    contact.Id
                ));
            }
        }

        if (contactCreateDto.UserId != Guid.Empty)
            contact.AssignUser(contactCreateDto.UserId);

        await _contactRepository.SaveAsync(contact);

        var planPrice = await _paymentPlanRepository.GetByIdAsync(contactCreateDto.PaymentPlanId);
        var payments = new List<Payment>();
        for (int i = 0; i < 12; i++)
        {
            payments.Add(new Payment(
                contact.Id,
                planPrice.Value,
                DateTime.Now.AddMonths(i),
                planPrice.Id
            ));
        }

        foreach (var payment in payments)
            await _paymentRepository.SaveAsync(payment);

        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<PagedResult<ContactDto>> GetPaginatedContactsAsync(Guid userId, int pageNumber, int pageSize, string searchTerm = "")
    {
        var contactsQuery = await _contactRepository.GetByUserAsync(userId);

        var filteredContacts = contactsQuery.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            filteredContacts = filteredContacts.Where(c =>
                c.FirstName.Contains(searchTerm) ||
                c.LastName.Contains(searchTerm) ||
                c.PrimaryEmail.Contains(searchTerm));
        }

        var totalItems = filteredContacts.Count();

        var contacts = filteredContacts
            .OrderBy(c => c.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var contactDTOs = contacts.Select(c => new ContactDto
        {
            Id = c.Id,
            FullName = c.GetFullName(),
            PrimaryEmail = c.PrimaryEmail,
            SecondaryEmail = c.SecondaryEmail,
            Phone = c.Phone,
            Mobile = c.Mobile,
            Department = c.Department,
            Title = c.Title,
            CreatedAt = c.CreatedAt
        }).ToList();

        return new PagedResult<ContactDto>
        {
            Items = contactDTOs,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> DeleteContactsAsync(List<Guid> ids)
    {
        var contacts = await _contactRepository.GetByIdsAsync(ids);
        if (!contacts.Any()) return false;

        var contactIds = contacts.Select(x => x.Id).ToList();

        foreach (var contact in contacts)
            _contactRepository.Delete(contact);

        var payments = await _paymentRepository.GetByContacts(contactIds);
        if (payments.Any())
        {
            foreach (var payment in payments)
                _paymentRepository.Delete(payment);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> UpdateContactAsync(ContactCreateDto contactCreateDto)
    {
        var contact = await _contactRepository.GetByIdAsync(contactCreateDto.Id);
        if (contact is null)
            return false;

        contact.Update(
            contactCreateDto.FirstName!,
            contactCreateDto.LastName!,
            contactCreateDto.Cpf!,
            contactCreateDto.PrimaryEmail!,
            contactCreateDto.SecondaryEmail!,
            contactCreateDto.Phone!,
            contactCreateDto.Mobile!,
            contactCreateDto.Department!,
            contactCreateDto.Title!
        );

        var contactIds = new List<Guid> { contact.Id };

        var payments = await _paymentRepository.GetByContacts(contactIds);
        var existingPaymentPlanId = payments.FirstOrDefault()?.PaymentPlanId;

        if (existingPaymentPlanId != contactCreateDto.PaymentPlanId)
        {
            foreach (var payment in payments)
                _paymentRepository.Delete(payment);

            var planPrice = await _paymentPlanRepository.GetByIdAsync(contactCreateDto.PaymentPlanId);
            var newPayments = new List<Payment>();
            for (int i = 0; i < 12; i++)
            {
                newPayments.Add(new Payment(
                    contact.Id,
                    planPrice.Value,
                    DateTime.Now.AddMonths(i),
                    planPrice.Id
                ));
            }

            foreach (var payment in newPayments)
                await _paymentRepository.SaveAsync(payment);
        }

        _contactRepository.Update(contact);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<ContactDetailsDto> GetContactDetailsAsync(Guid contactId)
    {
        var contact = await _contactRepository.GetByIdIncludeUserAsync(contactId);
        if (contact is null)
            throw new NotFoundException("Contato não encontrado.");

        var emails = new List<string> { contact.PrimaryEmail, contact.SecondaryEmail };
        var tickets = await _ticketRepository.GetByContactEmailAsync(emails);

        var totalTickets = tickets.Count();
        var onHoldTickets = tickets.Count(t => t.Status == TicketStatus.Onhold);

        var averageResponseTime = 0.0;
        if (tickets.Where(t => t.Messages != null && t.Messages.Any(m => m.UserId == null)).Count() > 0)
        {
            averageResponseTime = tickets
                .Where(t => t.Messages != null && t.Messages.Any(m => m.UserId == null))
                .Average(t => t.Messages!
                    .Where(m => m.UserId == null)
                    .Average(m => (m.SentAt - t.CreatedAt)?.TotalMinutes ?? 0));
        }

        var totalResponseTime = tickets
            .Where(t => t.Status == TicketStatus.Open)
            .Sum(t => (t.FirstResponseAt - t.CreatedAt)?.TotalHours ?? 0);

        var paymentActivities = await _paymentRepository.GetByContactIdAsync(contactId);

        DateTime? lastInteraction = null;
        var lastTicket = tickets.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        if (lastTicket != null)
        {
            var lastMessage = lastTicket.Messages?.Where(m => m.UserId == null).OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            if (lastMessage != null)
            {
                lastInteraction = lastMessage.CreatedAt;
            }
            else
            {
                lastInteraction = lastTicket.CreatedAt;
            }
        }

        var contactDetailsDto = new ContactDetailsDto
        {
            ContactId = contact.Id,
            FullName = $"{contact.FirstName} {contact.LastName}",
            Departament = contact.Department ?? "Sem departamento",
            ProfileImageUrl = "~/img/user-default.webp",
            Tag = "Tag exemplo",
            LastInteraction = lastInteraction,

            TotalTickets = totalTickets,
            OnHoldTickets = onHoldTickets,
            AverageResponseTime = TimeSpan.FromMinutes(averageResponseTime),
            TotalResponseTime = TimeSpan.FromHours(totalResponseTime),

            Source = "Fonte exemplo",
            PhoneNumber = contact.Phone,
            Email = contact.PrimaryEmail,

            ResponseTimeStatus = GetResponseTimeStatus(averageResponseTime),
            AssignedTo = contact.User?.FullName ?? "Não atribuído",
            FirstContactDate = contact.CreatedAt,

            PaymentActivities = paymentActivities.Select(p => new PaymentActivityDto
            {
                PaymentId = p.Id,
                Code = GeneratePaymentCode(p),
                Status = p.Paid ? "Concluído" : "Pendente",
                Method = p.PaymentPlan?.Description ?? "Desconhecido",
                PaymentDate = p.DueDate,
                Amount = (decimal)p.MonthlyFee
            }).OrderBy(x => x.PaymentDate.Month).ToList()
        };

        return contactDetailsDto;
    }

    private string GetResponseTimeStatus(double averageResponseTime)
    {
        if (averageResponseTime < 30)
            return "Rápido";
        else if (averageResponseTime < 60)
            return "Moderado";
        else
            return "Lento";
    }

    private string GeneratePaymentCode(Payment payment)
    {
        var monthNumber = payment.DueDate.Month.ToString("D2");
        var lastThreeChars = payment.Id.ToString().Substring(payment.Id.ToString().Length - 3);

        return $"{monthNumber}{lastThreeChars}";
    }

    public async Task<Guid?> GetContactPaymentPlanIdAsync(Guid contactId)
    {
        if (contactId == Guid.Empty) throw new EntityValidationException("O id do usuário não pode ser nulo.");

        var paymentPlanId = await _paymentRepository.GetPaymentPlanIdByContactIdAsync(contactId);

        if (!paymentPlanId.HasValue)
            throw new NotFoundException("Plano de pagamento não encontrado para o contato especificado.");

        return paymentPlanId;
    }

    public async Task<(int Total, int Onhold)> GetTotalAndOnholdTicketsAsyn(string contactEmail)
    {
        try
        {
            var (totalTicketsAtribuied, totalTicketsonHold) = await _ticketRepository.GetTicketAssocieteContactTotalAndOnhold(contactEmail);
            return (totalTicketsAtribuied, totalTicketsonHold);
        }
        catch (System.Exception)
        {
            throw;
        }

    }
}
