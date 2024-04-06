using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models.ReturnObjects
{
    public enum ServiceResponseCode
    {
        Success = 201,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        InternalServerError = 500
    }

    public class ServiceResult
    {
        protected string _Error { get; set; }

        protected ServiceResponseCode _ResponseCode = ServiceResponseCode.Success;

        public bool IsSuccessful
        {
            get
            {
                return _ResponseCode == ServiceResponseCode.Success;
            }
        }

        public string Error
        {
            get
            {
                return _Error ?? "";
            }
        }

        public ServiceResponseCode ResponseCode
        {
            get
            {
                return _ResponseCode;
            }
        }


        public static ServiceResult SuccessfulResult()
        {
            return new ServiceResult
            {
                _ResponseCode = ServiceResponseCode.Success
            };
        }

        public static ServiceResult FailedResult(string error, ServiceResponseCode code = ServiceResponseCode.InternalServerError)
        {
            return new ServiceResult
            {
                _ResponseCode = ServiceResponseCode.BadRequest,
                _Error = error
            };
        }

        private string ToJson()
        {
            return JsonConvert.SerializeObject(new { error = Error, success = IsSuccessful });
        }

        public IActionResult ActionResult
        {
            get
            {
                return new ContentResult {
                    StatusCode = (int)_ResponseCode,
                    ContentType = "application/json",
                    Content = ToJson(),
                };
            }
        }
    }

    public class ServiceResult<T> : ServiceResult
    { 
        private ServiceResult(ServiceResult result)
        {
            _Error = result.Error;
            _ResponseCode = result.ResponseCode;
        }

        public T Result { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(new { error = Error, success = IsSuccessful, result = Result });
        }

        public new IActionResult ActionResult
        {
            get
            {
                return new ContentResult {
                    StatusCode = (int)_ResponseCode,
                    ContentType = "application/json",
                    Content = ToJson()
                };
            }
        }

        public static ServiceResult<T> SuccessfulResult(T result)
        {
            return new ServiceResult<T>(SuccessfulResult())
            {
                Result = result
            };
        }

        public static ServiceResult<T> FailedResult(string error, ServiceResponseCode code = ServiceResponseCode.InternalServerError)
        {
            return new ServiceResult<T>(ServiceResult.FailedResult(error, code));
        }

        public static ServiceResult<T> ConvertResult(ServiceResult result)
        {
            return new ServiceResult<T>(result);
        }
    }
}
