using System.ComponentModel.DataAnnotations;

namespace WebApi_Templates.Models.ControllerModels;

public class Test2Model
{
    [Required]
    public long? id { get; set; }
}