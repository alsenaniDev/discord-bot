using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public PlansController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlans(
        CancellationToken cancellationToken)
    {
        var plans = await _subscriptionService.GetPlansAsync(cancellationToken);
        return Ok(plans);
    }
}
