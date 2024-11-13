using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Application.Services;
public class DepartmentService : Service<DepartmentDto, Department>, IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentMemberRepository _departmentMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(
        IRepository<Department> repository,
        IDepartmentRepository departmentRepository,
        IDepartmentMemberRepository departmentMemberRepository,
        IUnitOfWork unitOfWork
    )
        : base(repository)
    {
        _departmentRepository = departmentRepository;
        _departmentMemberRepository = departmentMemberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<DepartmentDto>> GetPaginatedDepartmentsAsync(int pageNumber, int pageSize, string searchTerm = "", int? status = null, string memberOrder = "")
    {
        var (departments, totalItems) = await _departmentRepository.GetPaginatedDepartmentsAsync(pageNumber, pageSize, searchTerm, status, memberOrder);

        var departmentDtos = new List<DepartmentDto>();

        foreach (var department in departments)
        {
            var members = await _departmentMemberRepository.GetMembersByDepartmentIdAsync(department.Id);
            var memberDtos = members.Select(m => new DepartmentMemberDto
            {
                Id = m.Id,
                FullName = m.FullName,
                ProfilePictureUrl = m.ProfilePictureUrl
            }).ToList();

            var departmentDto = new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Code = department.Code,
                Description = department.Description,
                Status = department.Status,
                MembersCount = department.Members.Count,
                Members = memberDtos.OrderBy(x => x.FullName).ToList()
            };

            departmentDtos.Add(departmentDto);
        }

        return new PagedResult<DepartmentDto>
        {
            Items = departmentDtos,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }


    public async Task<bool> CreateDepartmentAsync(DepartmentDto departmentDto)
    {
        var department = new Department(
            departmentDto.Name,
            departmentDto.Code,
            departmentDto.Description,
            departmentDto.Status
        );

        if (departmentDto.Members != null && departmentDto.Members.Any())
        {
            foreach (var memberDto in departmentDto.Members)
            {
                var departmentMember = memberDto.Type switch
                {
                    UserContactType.User => new DepartmentMember(department.Id, memberDto.Id, null),
                    UserContactType.Contact => new DepartmentMember(department.Id, null, memberDto.Id),
                    _ => throw new EntityValidationException("Tipo de usuário ou contato inválido.")
                };
                department.AddMember(departmentMember);
                await _departmentMemberRepository.SaveAsync(departmentMember);
            }
        }

        await _departmentRepository.SaveAsync(department);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> UpdateDepartmentAsync(DepartmentDto departmentDto)
    {
        if (departmentDto.Id == Guid.Empty)
            return false;

        var department = await _departmentRepository.GetByIdAsync(departmentDto.Id);

        if (department is null)
            throw new EntityValidationException("Departamento não encontrado.");

        department.UpdateDepartment(
            departmentDto.Name,
            departmentDto.Code,
            departmentDto.Description,
            departmentDto.Status
        );

        if (departmentDto.Members != null && departmentDto.Members.Any())
        {
            var members = await _departmentMemberRepository.GetByDepartmentIdAsync(department.Id);


            foreach (var member in members)
            {
                if (!departmentDto.Members.Any(m => m.Id == member.Id))
                {
                    department.RemoveMember(member);
                    _departmentMemberRepository.Delete(member);
                }
            }

            foreach (var memberDto in departmentDto.Members)
            {
                if (!members.Any(m => m.Id == memberDto.Id))
                {
                    var departmentMember = memberDto.Type switch
                    {
                        UserContactType.User => new DepartmentMember(department.Id, memberDto.Id, null),
                        UserContactType.Contact => new DepartmentMember(department.Id, null, memberDto.Id),
                        _ => throw new EntityValidationException("Tipo de usuário ou contato inválido.")
                    };
                    department.AddMember(departmentMember);
                    await _departmentMemberRepository.SaveAsync(departmentMember);
                }
            }
        }

        _departmentRepository.Update(department);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> DeleteDepartmentAsync(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        var department = await _departmentRepository.GetByIdAsync(id);

        if (department is null)
            return false;

        _departmentRepository.Delete(department);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<List<DepartmentMemberDto>> GetMembersByDepartmentIdAsync(Guid id)
    {
        var members = await _departmentMemberRepository.GetMembersByDepartmentIdAsync(id);
        var memberDtos = members.Select(m => new DepartmentMemberDto
        {
            Id = m.Id,
            FullName = m.FullName,
            ProfilePictureUrl = m.ProfilePictureUrl
        }).ToList();

        return memberDtos;
    }
}
