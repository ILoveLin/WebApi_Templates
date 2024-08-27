using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using WebApi_Templates.Models.Attributes;
using WebApi_Templates.Utils.NullUtils;

namespace WebApi_Templates.Models.Conventions;

public class ApiVersionsBuilder : ApiVersionConventionBuilder
{
    
    public override bool ApplyTo(ControllerModel controller)
    {
        var builder = this.Controller(controller.ControllerType);
        SetControllerApiVersions(builder,controller);

        for (int i = 0; i < controller.Actions.Count; i++)
        {
            SetActionMapToVersions(builder,controller.Actions[i]);
        }
        return base.ApplyTo(controller);
    }

    private void SetControllerApiVersions(IControllerConventionBuilder builder,ControllerModel controller)
    {
        var versions = controller.Attributes.OfType<ApiVersionsAttribute>().Select(a => a.versions).FirstOrDefault();
        if (!versions.IsNullOrEmpty())
        {
            for (var i = 0; i < versions.Length; i++)
            {
                builder.HasApiVersion(new ApiVersion(versions[i]));
            }
            return;
        }
        var supportAllVersions = controller.Attributes.OfType<AllApiVersionsAttribute>().Any();
        if (supportAllVersions)
        {
            for (var i = 0; i < App.ApiVersions.Count; i++)
            {
                builder.HasApiVersion(new ApiVersion(App.ApiVersions[i]));
            }
        }
    }
    
    private void SetActionMapToVersions(IControllerConventionBuilder controllerbuilder,ActionModel action)
    {
        var versions = action.Attributes.OfType<ApiVersionsAttribute>().Select(a => a.versions).FirstOrDefault();
        if (!versions.IsNullOrEmpty())
        {
            var builder = controllerbuilder.Action(action.ActionName);
            for (var i = 0; i < versions.Length; i++)
            {
                builder.HasApiVersion(new ApiVersion(versions[i]));
            }
        }
    }
}




