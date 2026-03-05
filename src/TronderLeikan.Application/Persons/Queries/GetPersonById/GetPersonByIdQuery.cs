using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonById;
public record GetPersonByIdQuery(Guid PersonId) : IQuery<PersonDetailResponse>;
