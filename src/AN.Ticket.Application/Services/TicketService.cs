﻿using AN.Ticket.Application.DTOs.Activity;
using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.DTOs.Email;
using AN.Ticket.Application.DTOs.SatisfactionRating;
using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.Helpers.EmailSender;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System.Globalization;
using System.Text;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Services;
public class TicketService
    : Service<TicketDto, DomainEntity.Ticket>, ITicketService
{
    private readonly BaseUrlSettings _baseUrlSettings;
    private readonly IRepository<DomainEntity.Ticket> _ticketService;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketMessageRepository _ticketMessageRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly ISatisfactionRatingRepository _satisfactionRatingRepository;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAssetAssignmentRepository _assetAssignmentRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public TicketService(
        IOptions<BaseUrlSettings> baseUrlSettings,
        IRepository<DomainEntity.Ticket> service,
        ITicketRepository ticketRepository,
        ITicketMessageRepository ticketMessageRepository,
        IActivityRepository activityRepository,
        ISatisfactionRatingRepository satisfactionRatingRepository,
        IEmailSenderService emailSenderService,
        IAttachmentRepository attachmentRepository,
        IUserRepository userRepository,
        IAssetAssignmentRepository assetAssignmentRepository,
        IContactRepository contactRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork
    )
        : base(service)
    {
        _baseUrlSettings = baseUrlSettings.Value;
        _ticketService = service;
        _ticketRepository = ticketRepository;
        _ticketMessageRepository = ticketMessageRepository;
        _activityRepository = activityRepository;
        _satisfactionRatingRepository = satisfactionRatingRepository;
        _emailSenderService = emailSenderService;
        _attachmentRepository = attachmentRepository;
        _userRepository = userRepository;
        _assetAssignmentRepository = assetAssignmentRepository;
        _contactRepository = contactRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GetImageBytesAsync(string relativePath)
    {
        using (HttpClient client = new HttpClient())
        {
            var imageUrl = _baseUrlSettings.BaseUrl + relativePath;
            var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new Exception("Image not found at URL: " + imageUrl);
            }
        }
    }

    public async Task<bool> CreateTicketAsync(CreateTicketDto createTicket)
    {
        var ticket = new DomainEntity.Ticket(
            createTicket.Name,
            createTicket.AccountName,
            createTicket.Email,
            createTicket.Phone,
            createTicket.Subject,
            createTicket.Status,
            createTicket.DueDate,
            createTicket.Priority
        );

        if (createTicket.UserId != Guid.Empty)
            ticket.AssignUsers(createTicket.UserId);

        var ticketMessage = new TicketMessage(
            createTicket.Description,
            DateTime.UtcNow.AddHours(-3)
        );

        if (createTicket.UserId != Guid.Empty)
            ticketMessage.AssignUser(createTicket.UserId);

        ticketMessage.AssignTicket(ticket.Id);

        var listTicketMessages = new List<TicketMessage> { ticketMessage };
        ticket.AddMessages(listTicketMessages);

        await _ticketMessageRepository.SaveAsync(ticketMessage);

        if (createTicket.AttachmentFile != null && createTicket.AttachmentFile.Length > 0)
        {
            if (createTicket.AttachmentFile.Length > 10485760)
            {
                throw new EntityValidationException("O arquivo excede o tamanho máximo permitido de 10 MB.");
            }

            using var memoryStream = new MemoryStream();
            await createTicket.AttachmentFile.CopyToAsync(memoryStream);

            var attachment = new Attachment(
                createTicket.AttachmentFile.FileName,
                memoryStream.ToArray(),
                createTicket.AttachmentFile.ContentType,
                ticket.Id,
                ticketMessage.Id
            );

            ticket.AddAttachment(attachment);

            await _attachmentRepository.SaveAsync(attachment);
        }

        await _ticketRepository.SaveAsync(ticket);
        await _unitOfWork.CommitAsync();

        var code = await _ticketRepository.GetTicketCodeByIdAsync(ticket.Id);

        string imagePathOnHold = ticket.Status == TicketStatus.Onhold ? "/img/status/onhold-on.png" : "/img/status/onhold-on.png";
        string imagePathOpen = ticket.Status == TicketStatus.Open ? "/img/status/open-on.png" : "/img/status/open-off.png";
        string imagePathInProgress = ticket.Status == TicketStatus.InProgress ? "/img/status/progress-on.png" : "/img/status/progress-off.png";
        string imagePathClosed = ticket.Status == TicketStatus.Closed ? "/img/status/close-on.png" : "/img/status/close-off.png";

        var imageBytesOnHold = await GetImageBytesAsync(imagePathOnHold);
        var imageBytesOpen = await GetImageBytesAsync(imagePathOpen);
        var imageBytesInProgress = await GetImageBytesAsync(imagePathInProgress);
        var imageBytesClosed = await GetImageBytesAsync(imagePathClosed);

        var imageCidOnHold = MimeUtils.GenerateMessageId();
        var imageCidOpen = MimeUtils.GenerateMessageId();
        var imageCidInProgress = MimeUtils.GenerateMessageId();
        var imageCidClosed = MimeUtils.GenerateMessageId();

        string htmlContent = $@"
            <html>
                <body>
                    <div style='max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                        <h2 style='color: #0056b3; text-align: center;'>Suporte ao Cliente</h2>
                        <h3>Olá {ticket.ContactName}</h3>
                        <p>Recebemos seu contato e um <strong>chamado de suporte</strong> foi aberto com sucesso! Nossa equipe está trabalhando para solucionar o seu problema o mais rápido possível.</p>
                        
                        <h3>Detalhes do Chamado:</h3>
                        <ul style='list-style-type: none; padding: 0;'>
                            <li><strong>Assunto:</strong> {ticket.Subject}</li>
                            <li><strong>Descrição:</strong> {createTicket.Description}</li>
                            <li><strong>Código do Chamado:</strong> #{code}</li>
                            <li><strong>Data de Abertura:</strong> {DateTime.Now}</li>
                        </ul>

                        <h3>Contatos para Suporte:</h3>
                        <p>Você também pode entrar em contato conosco pelos seguintes canais de atendimento:</p>
                        <ul style='list-style-type: none; padding: 0;'>
                            <li><strong>Email:</strong> suporte.anatlasnetwork@gmail.com</li>
                            <li><strong>Horário de Atendimento:</strong> Segunda a Sexta, das 8h às 18h</li>
                        </ul>
                       <div style='margin-top: 20px; position: relative; text-align: center;'>
                            <!-- Linha tracejada -->
                            
                            <!-- Tabela para alinhar os ícones e textos -->
                            <table style='width: 100%; z-index: 1; position: relative; background-color: #fff;'>
                                <tr>
                                    <!-- Status: Em espera -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidOnHold}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em espera</p>
                                    </td>

                                    <!-- Status: Aberto -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidOpen}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Aberto</p>
                                    </td>

                                    <!-- Status: Em progresso -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidInProgress}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em progresso</p>
                                    </td>

                                    <!-- Status: Fechado -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidClosed}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Fechado</p>
                                    </td>
                                </tr>
                            </table>
                        </div>


                        <p style='font-size: 14px; color: #777;'>Atenciosamente,<br>Equipe de Suporte - AtlasNetworks</p>

                        <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;' />
                        <p style='font-size: 12px; color: #aaa; text-align: center;'>Este é um e-mail automático, por favor, não responda diretamente a esta mensagem.</p>
                        
                    </div>
                </body>
            </html>";
        var imageMimeParts = new List<MimePart>
        {
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOnHold)),
                ContentId = imageCidOnHold,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOpen)),
                ContentId = imageCidOpen,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesInProgress)),
                ContentId = imageCidInProgress,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesClosed)),
                ContentId = imageCidClosed,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            }
        };
        await _emailSenderService.SendEmailAsync(
            ticket.Email,
            ticket.Subject,
            htmlContent,
            imageMimeParts
        );
        return true;
    }

    public async Task<IEnumerable<TicketDto>> GetTicketsByUserIdAsync(Guid userId)
    {
        var userTickets = await _ticketRepository.GetTicketsByUserIdAsync(userId);
        return userTickets.Select(t => new TicketDto
        {
            Id = t.Id,
            ContactName = t.ContactName,
            AccountName = t.AccountName,
            Email = t.Email,
            Phone = t.Phone,
            Subject = t.Subject,
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            ClosedAt = t.ClosedAt,
            FirstResponseAt = t.FirstResponseAt
        }).ToList();
    }

    public async Task<List<TicketDto>> GetTicketWithDetailsByUserAsync(Guid userId)
    {
        var ticket = await _ticketRepository.GetTicketWithDetailsByUserAsync(userId);
        if (ticket == null)
        {
            return null;
        }

        return _mapper.Map<List<TicketDto>>(ticket);
    }

    public async Task<IEnumerable<TicketDto>> GetTicketsNotAssignedAsync()
    {
        var tickets = await _ticketRepository.GetTicketsNotAssignedAsync();

        return tickets.Select(t => new TicketDto
        {
            Id = t.Id,
            ContactName = t.ContactName,
            AccountName = t.AccountName,
            Email = t.Email,
            Phone = t.Phone,
            Subject = t.Subject,
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            ClosedAt = t.ClosedAt,
            FirstResponseAt = t.FirstResponseAt
        }).ToList();
    }

    public async Task<bool> AssignTicketToUserAsync(Guid ticketId, Guid userId, bool byAdmin = false)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (!byAdmin && (ticket is null || ticket.UserId != null))
        {
            return false;
        }

        ticket.AssignUsers(userId);
        _ticketRepository.Update(ticket);

        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<TicketDetailsDto> GetTicketDetailsAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetTicketWithDetailsAsync(ticketId);
        if (ticket is null) return null;

        var ticketDetailsDto = new TicketDetailsDto
        {
            Ticket = _mapper.Map<TicketDto>(ticket),
        };

        foreach (var message in ticketDetailsDto.Ticket.Messages)
        {
            message.Attachments = ticketDetailsDto.Ticket.Attachments
                .Where(a => a.TicketMessageId == message.Id)
                .ToList();
        }

        var email = ticket.Email;
        var contact = await _contactRepository.GetByEmailAsync(email);

        if (contact is not null)
        {
            var assets = (await _assetAssignmentRepository
                .GetByContactIdAsync(contact.Id))
                .Select(assignment => assignment.Asset)
                .ToList();

            ticketDetailsDto.Ticket.Assets = _mapper.Map<List<AssetDto>>(assets);
        }

        return ticketDetailsDto;
    }

    public async Task<bool> ResolveTicketAsync(TicketResolutionDto ticketResolutionDto)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketResolutionDto.TicketId);
        if (ticket is null)
        {
            return false;
        }

        ticket.SetResolution(ticketResolutionDto.ResolutionDetails);
        if (
            ticket.FirstResponseAt is null &&
            ticket.FirstResponseAt != DateTime.MinValue
        )
            ticket.RecordFirstResponse();

        ticket.CloseTicket();

        _ticketRepository.Update(ticket);

        await _unitOfWork.CommitAsync();

        if (ticketResolutionDto.NotifyContact)
        {
            string imagePathOnHold = ticket.Status == TicketStatus.Onhold ? "/img/status/onhold-on.png" : "/img/status/onhold-on.png";
            string imagePathOpen = ticket.Status == TicketStatus.Open ? "/img/status/open-on.png" : "/img/status/open-on.png";
            string imagePathInProgress = ticket.Status == TicketStatus.InProgress ? "/img/status/progress-on.png" : "/img/status/progress-on.png";
            string imagePathClosed = ticket.Status == TicketStatus.Closed ? "/img/status/close-on.png" : "/img/status/close-on.png";

            var imageBytesOnHold = await GetImageBytesAsync(imagePathOnHold);
            var imageBytesOpen = await GetImageBytesAsync(imagePathOpen);
            var imageBytesInProgress = await GetImageBytesAsync(imagePathInProgress);
            var imageBytesClosed = await GetImageBytesAsync(imagePathClosed);

            var imageCidOnHold = MimeUtils.GenerateMessageId();
            var imageCidOpen = MimeUtils.GenerateMessageId();
            var imageCidInProgress = MimeUtils.GenerateMessageId();
            var imageCidClosed = MimeUtils.GenerateMessageId();

            string emailContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                <div style='max-width: 600px; margin: auto; border-radius: 10px; background-color: #ffffff; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Ticket Resolvido</h2>
                    <p>Olá {ticket.ContactName},</p>
                    <p>Informamos que o ticket com o código <strong>#{ticket.TicketCode}</strong> foi resolvido com sucesso.</p>
                    <p>Detalhes da Resolução:</p>
                    <blockquote style='margin: 20px 0; padding: 10px; background-color: #f1f9ff; border-left: 4px solid #0056b3;'>
                        {ticketResolutionDto.ResolutionDetails}
                    </blockquote>
                    <p>Data de Resolução: {ticket.ClosedAt?.ToString("dd/MM/yyyy HH:mm:ss")}</p>

                    <div style='margin-top: 20px; position: relative; text-align: center;'>
                        
                        <!-- Tabela para alinhar os ícones e textos -->
                        <table style='width: 100%; z-index: 1; position: relative; background-color: transparent;'>
                            <tr>
                                <!-- Status: Em espera -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidOnHold}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em espera</p>
                                    </div>
                                </td>

                                <!-- Status: Aberto -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidOpen}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Aberto</p>
                                    </div>
                                </td>

                                <!-- Status: Em progresso -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidInProgress}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em progresso</p>
                                    </div>
                                </td>

                                <!-- Status: Fechado -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidClosed}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Fechado</p>
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </div>";

            bool existsSatisfactionRating = await _satisfactionRatingRepository.ExistsByTicketIdAsync(ticket.Id);
            if (!existsSatisfactionRating)
            {
                var satisfactionRatingContent = GenerateSatisfactionRatingEmailContent(ticket.Id);
                emailContent += $@"
                        <hr style='border: 0; border-top: 1px solid #eee;' />
                        <p>Sua opinião é muito importante para nós! Por favor, avalie nosso atendimento:</p>
                        {satisfactionRatingContent}";
            }

            emailContent += @"
                        <hr style='border: 0; border-top: 1px solid #eee;' />
                        <p style='font-size: 14px; color: #777;'>Atenciosamente,<br />Equipe de Suporte</p>
                    </div>
                </body>
            </html>";

            var imageMimeParts = new List<MimePart>
            {
                new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(imageBytesOnHold)),
                    ContentId = imageCidOnHold,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                },
                new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(imageBytesOpen)),
                    ContentId = imageCidOpen,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                },
                new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(imageBytesInProgress)),
                    ContentId = imageCidInProgress,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                },
                new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(imageBytesClosed)),
                    ContentId = imageCidClosed,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                }
            };

            await _emailSenderService.SendEmailAsync(
                 ticket.Email,
                 "Ticket Resolvido",
                 emailContent,
                 imageMimeParts
             );
        }

        return true;
    }

    public async Task<bool> ReplyToTicketAsync(Guid ticketId, string messageText, Guid userId, List<IFormFile> attachments)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        var ticketMessage = new TicketMessage(messageText, DateTime.UtcNow.ToLocalTime());
        ticketMessage.AssignUser(userId);

        if (
            ticket.Status != TicketStatus.Closed ||
            ticket.Status != TicketStatus.InProgress ||
            ticket.Status != TicketStatus.Open
        )
            ticket.UpdateStatus(TicketStatus.Open);

        ticketMessage.AssignTicket(ticketId);
        ticket.AddMessages(new List<TicketMessage> { ticketMessage });

        var emailAttachments = new List<EmailAttachment>();
        if (attachments != null && attachments.Any())
        {
            foreach (var file in attachments)
            {
                if (file.Length > 10485760)
                {
                    throw new EntityValidationException("O arquivo excede o tamanho máximo permitido de 10 MB.");
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                var attachment = new Attachment(file.FileName, memoryStream.ToArray(), file.ContentType, ticket.Id);
                ticket.AddAttachment(attachment);
                attachment.AssignToMessage(ticketMessage.Id);
                emailAttachments.Add(new EmailAttachment(file.FileName, memoryStream.ToArray(), file.ContentType));
                await _attachmentRepository.SaveAsync(attachment);
            }
        }

        await _ticketMessageRepository.SaveAsync(ticketMessage);
        _ticketRepository.Update(ticket);
        await _unitOfWork.CommitAsync();

        string imagePathOnHold = ticket.Status == TicketStatus.Onhold ? "/img/status/onhold-on.png" : "/img/status/onhold-on.png";
        string imagePathOpen = ticket.Status == TicketStatus.Open ? "/img/status/open-on.png" : "/img/status/open-on.png";
        string imagePathInProgress = ticket.Status == TicketStatus.InProgress ? "/img/status/progress-on.png" : "/img/status/progress-off.png";
        string imagePathClosed = ticket.Status == TicketStatus.Closed ? "/img/status/close-on.png" : "/img/status/close-off.png";

        var imageBytesOnHold = await GetImageBytesAsync(imagePathOnHold);
        var imageBytesOpen = await GetImageBytesAsync(imagePathOpen);
        var imageBytesInProgress = await GetImageBytesAsync(imagePathInProgress);
        var imageBytesClosed = await GetImageBytesAsync(imagePathClosed);

        var imageCidOnHold = MimeUtils.GenerateMessageId();
        var imageCidOpen = MimeUtils.GenerateMessageId();
        var imageCidInProgress = MimeUtils.GenerateMessageId();
        var imageCidClosed = MimeUtils.GenerateMessageId();

        string emailContent = $@"
        <html>
            <body style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                <div style='max-width: 600px; margin: auto; border-radius: 10px; background-color: #ffffff; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Resposta ao seu Ticket</h2>
                    <p>Olá {ticket.ContactName},</p>
                    <p>Recebemos uma nova resposta ao seu ticket com o código <strong>#{ticket.TicketCode}</strong>:</p>
                    <blockquote style='margin: 20px 0; padding: 10px; background-color: #f1f9ff; border-left: 4px solid #0056b3;'>
                        {messageText}
                    </blockquote>
                    <p>Se precisar de mais informações ou tiver dúvidas, basta responder este e-mail. Nossa equipe está pronta para ajudar!</p>

                    <div style='margin-top: 20px; position: relative; text-align: center;'>
                        <!-- Linha tracejada -->
    
                        <!-- Tabela para alinhar os ícones e textos -->
                        <table style='width: 100%; z-index: 1; position: relative; background-color: transparent;'>
                            <tr>
                                <!-- Status: Em espera -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidOnHold}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em espera</p>
                                    </div>
                                </td>

                                <!-- Status: Aberto -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidOpen}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Aberto</p>
                                    </div>
                                </td>

                                <!-- Status: Em progresso -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidInProgress}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em progresso</p>
                                    </div>
                                </td>

                                <!-- Status: Fechado -->
                                <td style='text-align: center; color: #aaa; padding: 0 10px; position: relative;'>
                                    <div style='position: relative; z-index: 2;'>
                                        <img src='cid:{imageCidClosed}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Fechado</p>
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </div>

                    <hr style='border: 0; border-top: 1px solid #ddd; margin: 30px 0;' />
                    <p style='font-size: 14px; color: #777;'>Atenciosamente,<br />Equipe de Suporte</p>
                    <p style='font-size: 12px; color: #aaa; text-align: center;'>Este é um e-mail automático. Por favor, não responda diretamente.</p>
                </div>
            </body>
        </html>";

        var imageMimeParts = new List<MimePart>
        {
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOnHold)),
                ContentId = imageCidOnHold,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOpen)),
                ContentId = imageCidOpen,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesInProgress)),
                ContentId = imageCidInProgress,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesClosed)),
                ContentId = imageCidClosed,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            }
        };

        await _emailSenderService.SendEmailResponseAsync(
            ticket.Email,
            $"{ticket.Subject}",
            emailContent,
            ticket.EmailMessageId!,
            emailAttachments,
            imageMimeParts
        );

        return true;
    }

    public async Task<bool> DeleteTicketAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        _ticketRepository.Delete(ticket);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> UpdateTicketAsync(EditTicketDto editTicketDto)
    {
        var ticket = await _ticketRepository.GetByIdAsync(editTicketDto.Id);
        if (ticket is null)
        {
            return false;
        }

        if (ticket.Status == TicketStatus.Closed)
            throw new EntityValidationException("Não é possível editar um ticket fechado.");

        ticket.Update(
            editTicketDto.Status,
            editTicketDto.Priority,
            editTicketDto.DueDate,
            editTicketDto.ContactName,
            editTicketDto.AccountName,
            editTicketDto.Email,
            editTicketDto.Phone
        );

        _ticketRepository.Update(ticket);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<SupportDashboardDto> GetSupportDashboardAsync(
        Guid userId,
        TicketFilterDto filters,
        int pageNumber,
        int pageSize,
        int activityPageNumber,
        int activityPageSize
    )
    {
        var tickets = await _ticketRepository.GetTicketWithDetailsByUserAsync(userId);
        var activities = tickets.SelectMany(t => t.Activities);

        int openActivities = activities.Count(a => a.Status == ActivityStatus.Open);
        int closedActivities = activities.Count(a => a.Status == ActivityStatus.Closed);

        int totalPaddingTickets = tickets.Count(t =>
            (t.Status == TicketStatus.Onhold || t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
            t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.UserId == userId
        );

        var today = DateTime.UtcNow.Date;
        var currentPeriodStartDate = today.AddDays(-29);
        var previousPeriodStartDate = currentPeriodStartDate.AddDays(-30);
        var previousPeriodEndDate = currentPeriodStartDate.AddDays(-1);

        var previousPeriodTickets = tickets.Where(t => t.CreatedAt.Date >= previousPeriodStartDate && t.CreatedAt.Date <= previousPeriodEndDate).ToList();
        var currentPeriodTickets = tickets.Where(t => t.CreatedAt.Date >= currentPeriodStartDate && t.CreatedAt.Date <= today).ToList();

        int currentPendingTickets = currentPeriodTickets.Count(t =>
            (t.Status == TicketStatus.Onhold || t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
            t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.UserId == userId
        );

        int previousPendingTickets = previousPeriodTickets.Count(t =>
            (t.Status == TicketStatus.Onhold || t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
            t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.UserId == userId
        );

        var monthlyTickets = Enumerable.Range(1, 12).Select(month =>
        {
            var ticketsInMonth = tickets.Where(t => t.CreatedAt.Month == month);

            var responseTimes = ticketsInMonth
                .Select(t => t.GetTimeToFirstResponse())
                .Where(r => r.HasValue)
                .Select(r => r.Value.TotalHours);

            double averageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0;

            return new MonthlyTicketDataDto
            {
                Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month).Substring(0, 3),
                AverageResponseTimeHours = averageResponseTime
            };
        }).ToList();

        for (int i = 1; i < monthlyTickets.Count; i++)
        {
            var current = monthlyTickets[i].AverageResponseTimeHours;
            var previous = monthlyTickets[i - 1].AverageResponseTimeHours;

            if (previous == 0)
            {
                monthlyTickets[i].PercentageChange = current > 0 ? 100 : 0;
            }
            else
            {
                double change = ((current - previous) / previous) * 100;
                monthlyTickets[i].PercentageChange = change;
            }
        }

        if (monthlyTickets.Any())
        {
            monthlyTickets[0].PercentageChange = null;
        }

        var recentTickets = await GetRecentTicketsAsync(
            userId, filters, pageNumber, pageSize
        );

        var pagedActivities = activities
            .Where(a => a.Status == ActivityStatus.Open)
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.ScheduledDate)
            .Skip((activityPageNumber - 1) * activityPageSize)
            .Take(activityPageSize)
            .Select(a => new ActivitySummaryDto
            {
                Id = a.Id,
                Subject = a.Subject,
                Description = a.Description,
                ScheduledDate = a.ScheduledDate,
                Status = a.Status
            })
            .ToList();

        var totalRecentRatings = tickets.Select(t => t.SatisfactionRating).Count(r => r != null);

        var recentRatings = tickets
            .Where(t => t.SatisfactionRating != null)
            .OrderByDescending(t => t.SatisfactionRating.CreatedAt)
            .Take(2)
            .Select(t => new SatisfactionRatingSummaryDto
            {
                Rating = t.SatisfactionRating.Rating ?? SatisfactionRatingValue.VeryDissatisfied,
                Comment = t.SatisfactionRating.Comment ?? "",
                TicketId = t.SatisfactionRating.TicketId,
                TicketSubject = t.Subject,
                CreatedAt = t.SatisfactionRating.CreatedAt
            }).ToList();

        var totalActivities = activities.Count(a => a.Status == ActivityStatus.Open);

        var pagedActivityResult = new PagedResult<ActivitySummaryDto>
        {
            Items = pagedActivities,
            TotalItems = totalActivities,
            PageNumber = activityPageNumber,
            PageSize = activityPageSize
        };

        return new SupportDashboardDto
        {
            TotalPendingTickets = totalPaddingTickets,
            CurrentPendingTickets = currentPendingTickets,
            PreviousPendingTickets = previousPendingTickets,
            OpenActivities = openActivities,
            ClosedActivities = closedActivities,
            TotalRecentRatings = totalRecentRatings,
            RecentTickets = recentTickets,
            MonthlyTickets = monthlyTickets,
            PagedActivities = pagedActivityResult,
            RecentRatings = recentRatings
        };
    }

    public async Task<PagedResult<TicketSummaryDto>> GetRecentTicketsAsync(
        Guid userId, TicketFilterDto filters, int pageNumber, int pageSize
    )
    {
        var tickets = await _ticketRepository.GetTicketWithDetailsByUserAsync(userId);

        var filteredTickets = tickets.AsQueryable();

        if (filters.Status.HasValue)
            filteredTickets = filteredTickets.Where(t => t.Status == filters.Status.Value);

        if (filters.Priority.HasValue)
            filteredTickets = filteredTickets.Where(t => t.Priority == filters.Priority.Value);

        if (filters.DateFrom.HasValue)
            filteredTickets = filteredTickets.Where(t => t.DueDate.Date >= filters.DateFrom.Value.Date);

        if (filters.DateTo.HasValue)
            filteredTickets = filteredTickets.Where(t => t.DueDate.Date <= filters.DateTo.Value.Date);

        var query = filteredTickets
            .Where(t => t.Messages != null && t.Messages.Any())
            .ToList()
            .Where(t =>
            {
                var lastMessage = t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                return lastMessage != null && (lastMessage.UserId == Guid.Empty || lastMessage.UserId == null);
            })
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate);

        var totalItems = query.Count();
        var recentTickets = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketSummaryDto
            {
                Id = t.Id,
                Subject = t.Subject,
                ContactName = t.ContactName,
                Priority = t.Priority,
                DueDate = t.DueDate,
                Status = t.Status
            })
            .ToList();

        return new PagedResult<TicketSummaryDto>
        {
            Items = recentTickets,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private decimal CalculateChangePercentage(int current, int previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }

        var percentageChange = ((current - previous) / (decimal)previous) * 100;

        if (percentageChange > 500)
        {
            return 500;
        }

        return percentageChange;
    }

    public async Task<IEnumerable<TicketContactDetailsDto>> GetTicketsByContactIdAsync(List<string> emails, bool showAll)
    {
        var tickets = await _ticketRepository.GetByContactEmailAsync(emails);
        if (!showAll)
        {
            tickets = tickets.OrderByDescending(t => t.CreatedAt).Take(1).ToList();
        }

        tickets = tickets.Where(t => t.Status != TicketStatus.Closed).ToList();

        return tickets.Select(t => new TicketContactDetailsDto
        {
            TicketId = t.Id,
            TicketCode = t.TicketCode.ToString(),
            TicketTitle = t.Subject,
            TicketStatus = t.Status,
            TicketType = t.Classification ?? "Sem classificação",
            Priority = t.Priority,
            AssignedTo = t.User?.FullName ?? "Não atribuído",
            RequestDate = t.CreatedAt
        });
    }

    public async Task<Guid> GetUserWithLeastTicketsAsync()
    {
        var technicianTicketCounts = await _ticketRepository.GetUserTicketCountsAsync();

        var technicianWithLeastTickets = technicianTicketCounts.OrderBy(t => t.TicketCount).FirstOrDefault();
        return technicianWithLeastTickets?.UserId ?? Guid.Empty;
    }

    private string GenerateSatisfactionRatingEmailContent(Guid ticketId)
    {
        try
        {
            var emailContent = new StringBuilder();
            emailContent.AppendLine("<p style='color: #0056b3;'>Sua opinião é importante para nós!</p>");

            var ratingUrl = $"{_baseUrlSettings.BaseUrl}/SatisfactionRating/Index?ticketId={ticketId}";

            emailContent.AppendLine($"<p><a href=\"{ratingUrl}\" style='color: #0056b3; text-decoration: none; font-weight: bold;'>Clique aqui para nos avaliar</a></p>");
            emailContent.AppendLine("<p>Agradecemos pela sua colaboração e ficamos à disposição para ajudá-lo sempre que precisar.</p>");

            return emailContent.ToString();
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }
}