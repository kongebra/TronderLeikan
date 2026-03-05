using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersons;
public record GetPersonsQuery : IQuery<PersonSummaryResponse[]>;
