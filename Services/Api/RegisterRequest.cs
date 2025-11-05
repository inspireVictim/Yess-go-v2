using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YessGoFront.Services.Api
{
    namespace YessGoFront.Services.Api
    {
        /// <summary>
        /// Модель для отправки данных регистрации на backend
        /// ДОЛЖНА соответствовать app.schemas.user.UserCreate на сервере.
        /// Backend ждёт поля:
        /// phone_number, password, first_name, last_name
        /// </summary>
        public class RegisterRequest
        {
            public string phone_number { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public string first_name { get; set; } = string.Empty;
            public string last_name { get; set; } = string.Empty;
        }
    }


}
