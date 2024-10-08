﻿using AN.Ticket.Application.DTOs.Contact;
using AN.Ticket.Application.Extensions;
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
                contactCreateDto.PaymentPlanId
            ));
        }

        foreach (var payment in payments)
            await _paymentRepository.SaveAsync(payment);

        await _unitOfWork.CommitAsync();

        return true;
    }
}
