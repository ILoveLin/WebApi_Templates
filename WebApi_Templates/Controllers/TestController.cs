using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebApi_Templates.Controllers.BaseController;
using WebApi_Templates.Models.Attributes;
using WebApi_Templates.Models.ControllerModels;
using WebApi_Templates.Models.ResponseModels;
using WebApi_Templates.Utils.NullUtils;
using WebApi_Templates.Utils.ResponseUtils;

namespace WebApi_Templates.Controllers;

public class TestController : BaseController<TestController>
{
    public TestController(ILogger<TestController> _logger, IStringLocalizer<I18N> _localizer) : base(_logger, _localizer)
    {
    }

    [HttpGet]
    [Route("test")]
    [ApiVersions(1.0)]
    public async Task<IActionResult> testv1()
    {
         return ResponseUtil.ResponseMsg(ResponseCode.Success, localizer["请求成功！"] + "v1.0");
    }

    [HttpGet]
    [Route("test")]
    [ApiVersions([1.1])]
    public async Task<IActionResult> test2([FromQuery] Test2Model model)
    {
        return ResponseUtil.Response(ResponseCode.Success, new { model.id }, localizer["请求成功！"] + "v1.1");
    }

    [HttpGet]
    [Route("test")]
    public async Task<IActionResult> test3([FromQuery] Test3Model model)
    {
        return ResponseUtil.Response(ResponseCode.Success, new { model.str, model.id }, localizer["请求成功！"]);
    }
    
    [HttpGet]
    [Route("sendAll")]
    public async Task<IActionResult> sendAll([FromQuery] string data)
    {
        await App.WebSocketServerManager.SendAllAsync(data);
        return ResponseUtil.Response(ResponseCode.Success);
    }
}