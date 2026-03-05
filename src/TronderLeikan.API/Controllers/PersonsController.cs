using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Commands.CreatePerson;
using TronderLeikan.Application.Persons.Commands.DeletePerson;
using TronderLeikan.Application.Persons.Commands.DeletePersonImage;
using TronderLeikan.Application.Persons.Commands.UpdatePerson;
using TronderLeikan.Application.Persons.Commands.UploadPersonImage;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.API.Controllers;

// Kontroller for personressurser — CRUD og bildebehandling
public sealed class PersonsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PersonSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await sender.Query(new GetPersonsQuery(), ct)).Match(Ok, Problem);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await sender.Query(new GetPersonByIdQuery(id), ct)).Match(Ok, Problem);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreatePersonCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdatePersonCommand command, CancellationToken ct) =>
        (await sender.Send(command with { PersonId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        (await sender.Send(new DeletePersonCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Laster opp profilbilde for en person
    [HttpPut("{id:guid}/image")]
    public async Task<ActionResult> UploadImage(Guid id, IFormFile image, CancellationToken ct)
    {
        await using var ms = await ToMemoryStreamAsync(image, ct);
        var result = await sender.Send(new UploadPersonImageCommand(id, ms), ct);
        return result.Match<ActionResult>(() => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult> DeleteImage(Guid id, CancellationToken ct) =>
        (await sender.Send(new DeletePersonImageCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);
}
