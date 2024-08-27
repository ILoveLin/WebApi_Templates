using System.ComponentModel.DataAnnotations;

namespace WebApi_Templates.Models.ControllerModels;

public class Test3Model
{
    [Required]
    public string? str { get; set; }

    public long? id { get; set; }
}