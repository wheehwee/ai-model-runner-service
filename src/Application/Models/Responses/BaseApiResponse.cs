using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Responses
{
    public class BaseApiResponse<TData, TError> where TData : class 
        where TError : class
    {
        public TData? Data { get; set; }
        public TError? ErrorData { get; set; }
        public string Message { get; set; }
    }
}
