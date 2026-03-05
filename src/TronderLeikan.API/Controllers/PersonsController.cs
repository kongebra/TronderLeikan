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
public sealed class PersonsController(
    ICommandHandler<CreatePersonCommand, Guid> createHandler,
    ICommandHandler<UpdatePersonCommand> updateHandler,
    ICommandHandler<DeletePersonCommand> deleteHandler,
    ICommandHandler<UploadPersonImageCommand> uploadImageHandler,
    ICommandHandler<DeletePersonImageCommand> deleteImageHandler,
    IQueryHandler<GetPersonsQuery, PersonSummaryResponse[]> getPersonsHandler,
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse> getPersonByIdHandler)
    : ApiControllerBase
{
    // Henter alle personer
    [HttpGet]
    public async Task<ActionResult<PersonSummaryResponse[]>> GetAll(CancellationToken ct) =>
        (await getPersonsHandler.Handle(new GetPersonsQuery(), ct)).Match(Ok, Problem);

    // Henter én person basert på id
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid id, CancellationToken ct) =>
        (await getPersonByIdHandler.Handle(new GetPersonByIdQuery(id), ct)).Match(Ok, Problem);

    // Oppretter en ny person og returnerer 201 med id
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreatePersonCommand command, CancellationToken ct)
    {
        var result = await createHandler.Handle(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id }, id),
            Problem);
    }

    // Oppdaterer en eksisterende person
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdatePersonCommand command, CancellationToken ct) =>
        (await updateHandler.Handle(command with { PersonId = id }, ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Sletter en person
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        (await deleteHandler.Handle(new DeletePersonCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);

    // Laster opp profilbilde for en person — kopiér til MemoryStream for sikker livstidsstyring
    [HttpPut("{id:guid}/image")]
    public async Task<ActionResult> UploadImage(Guid id, IFormFile image, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        ms.Position = 0;
        var result = await uploadImageHandler.Handle(new UploadPersonImageCommand(id, ms), ct);
        return result.Match<ActionResult>(() => NoContent(), Problem);
    }

    // Sletter profilbilde for en person
    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult> DeleteImage(Guid id, CancellationToken ct) =>
        (await deleteImageHandler.Handle(new DeletePersonImageCommand(id), ct)).Match<ActionResult>(() => NoContent(), Problem);
}
