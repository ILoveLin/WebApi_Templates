
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebApi_Templates.Models.Filters;


[Serializable, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NoNeedLoginAttribute : Attribute, IFilterMetadata, IOrderedFilter
{
    public int Order => 1003;
}

[Serializable, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NeedLoginAttribute : Attribute, IActionFilter, IOrderedFilter
{
    public int Order => 3;
    
    public NeedLoginAttribute()
    {
        
    }
    
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
    
    public void OnActionExecuting(ActionExecutingContext context)
    {
        IList<IFilterMetadata> filters = context.Filters;
        foreach (IFilterMetadata filter in filters)
        {
            if (filter is NoNeedLoginAttribute)
            {
                return;
            }
        }

        // UserClaims userClaims =  CheckAuthenticateAsync(context.HttpContext.User,ctx).GetAwaiter().GetResult();
        // if (userClaims != null)
        // {
        //     context.HttpContext.Items.TryAdd("userClaims", userClaims);
        //     return;
        // }
        // else
        // {
        //     context.Result = new OkObjectResult(ResponseUtil.LoginInvalid());
        // }
    }
    
    // public async Task<UserClaims> CheckAuthenticateAsync(ClaimsPrincipal claimsPrincipal, CmeLiveContext ctx)
    // {
    //     if (claimsPrincipal.Identity.IsAuthenticated)
    //     {
    //         UserClaims userClaims = new UserClaims();
    //         //todo: 此处需要查询user表查询isuse字段
    //         var list = claimsPrincipal.Claims.ToList();
    //         foreach (var iter in list)
    //         {
    //             switch (iter.Type)
    //             {
    //                 case "id":
    //                 {
    //                     userClaims.id = long.Parse(iter.Value);
    //                     break;
    //                 }
    //                 case "roleID":
    //                 {
    //                     userClaims.roleID = long.Parse(iter.Value);
    //                     break;
    //                 }
    //                 case "brand":
    //                 {
    //                     userClaims.brand = int.Parse(iter.Value);
    //                     break;
    //                 }
    //                 case "username":
    //                 {
    //                     userClaims.username = iter.Value;
    //                     break;
    //                 }
    //                 case "nickname":
    //                 {
    //                     userClaims.nickname = iter.Value;
    //                     break;
    //                 }
    //                 case "amNumber":
    //                 {
    //                     userClaims.amNumber = iter.Value != null ? long.Parse(iter.Value) : null;
    //                     break;
    //                 }
    //             }
    //         }
    //
    //         bool flag = await ctx.Users.AnyAsync(a => a.Id == userClaims.id && a.IsUse == true && a.IsDelete == false);
    //         return flag ? userClaims : null;
    //     }
    //
    //     return null;
    // }
    

}