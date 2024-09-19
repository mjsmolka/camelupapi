using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using CamelUpAutomation.Models.Configuration;
using CamelUpAutomation.Models.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static CamelUpAutomation.Services.ValidatorService;
using CamelUpAutomation.Models.ReturnObjects;

namespace CamelUpAutomation.Services
{
    public interface IValidatorService
    {
        public ServiceResult<T> ValidateDto<T>(T dto);
    }

    public class ValidatorService : IValidatorService
    {
        // generate a static class to generate a random string with letters and numbers 
        public ServiceResult<T> ValidateDto<T>(T dto)
        {
            try
            {
                Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);
                return ServiceResult<T>.SuccessfulResult(dto);
            } catch (ValidationException exception)
            {
                return ServiceResult<T>.FailedResult(exception.Message, ServiceResponseCode.BadRequest);
            }
        }
    }
}
