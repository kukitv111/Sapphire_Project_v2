[Authorize]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public SessionController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize]
    public async Task<Result<SessionDto>> StartSession(StartSessionCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpPost("end")]
    [Authorize]
    public async Task<Result> EndSession()
    {
        var command = new EndSessionCommand(_currentUserService.UserId);
        return await _mediator.Send(command);
    }
}
