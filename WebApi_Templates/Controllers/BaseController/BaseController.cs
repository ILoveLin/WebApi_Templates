using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebApi_Templates.Models.Attributes;

namespace WebApi_Templates.Controllers.BaseController;

[ApiController]
[Route("api/[controller]")]
[AllApiVersions]
public class BaseController<T> : ControllerBase
{
    protected readonly IStringLocalizer<I18N> localizer;
    protected readonly ILogger<T> logger;

    public BaseController(ILogger<T> _logger, IStringLocalizer<I18N> _localizer)
    {
        logger = _logger;
        localizer = _localizer;
    }
}