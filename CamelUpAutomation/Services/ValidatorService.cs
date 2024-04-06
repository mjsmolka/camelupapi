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
        public ServiceResult ValidateHttpRequest(HttpRequest req);

        public Task<ServiceResult<T>> ValidateDto<T>(HttpRequest req);
    }

    public class ValidatorService : IValidatorService
    {
        // generate a static class to generate a random string with letters and numbers 
        public async Task<ServiceResult<T>> ValidateDto<T>(HttpRequest req)
        {
            try
            {
                var options = new JsonSerializerOptions();
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                T reqBody = await req.ReadFromJsonAsync<T>(options);
                Validator.ValidateObject(reqBody, new ValidationContext(reqBody), validateAllProperties: true);
                return ServiceResult<T>.SuccessfulResult(reqBody);
            } catch (ValidationException exception)
            {
                return ServiceResult<T>.FailedResult(exception.Message, ServiceResponseCode.BadRequest);
            }
        }

        public ServiceResult ValidateHttpRequest(HttpRequest req)
        {
            if (!req.IsJsonContentType())
            {
                return ServiceResult.FailedResult("Cannot deserialize request because it does not represent a valid JSON content");
            }
            if (req.ContentLength >= 1024)
            {
                 return ServiceResult.FailedResult("Cannot deserialize request because its content is bigger than expected");
            }
            return null;
        }
    }
}
