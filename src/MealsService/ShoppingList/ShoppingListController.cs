using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using MealsService.Responses;
using MealsService.ShoppingList.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.ShoppingList
{
    [Route("[controller]")]
    public class ShoppingListController : Controller
    {
        private ShoppingListService _shoppingListService { get; }

        public ShoppingListController(ShoppingListService shoppingListService)
        {
            _shoppingListService = shoppingListService;
        }

        [Authorize]
        [Route("me/items"), HttpGet]
        [Route("me/{dateString:datetime}/items"), HttpGet]
        public IActionResult Get(string dateString = "")
        {
            var claims = HttpContext.User.Claims;
            int id = 0;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out id);

            return Get(id, dateString);
        }

        [Route("{userId:int}/items"), HttpGet]
        [Route("{userId:int}/{dateString:datetime}/items"), HttpGet]
        public IActionResult Get(int userId, string dateString = "")
        {
            var claims = HttpContext.User.Claims.ToList();
            int authorizedId = 0;
            bool isAdmin = false;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out authorizedId);
            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);

            if (userId != authorizedId && !isAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Json(new ErrorResponse("Not authorized to make this request", (int)HttpStatusCode.Forbidden));
            }

            DateTime date;
            if (dateString == "")
            {
                date = DateTime.Now.Date;
            }
            else
            {
                var dateParts = dateString.Split('-');
                if (dateParts.Length != 3)
                {
                    Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-mm-dd)", (int)HttpStatusCode.BadRequest));
                }

                int year, month, day;
                if (!int.TryParse(dateParts[0], out year)
                    || !int.TryParse(dateParts[1], out month)
                    || !int.TryParse(dateParts[2], out day))
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-mm-dd)", (int)HttpStatusCode.BadRequest));
                }
                date = new DateTime(year, month, day);
            }

            var days = (int)date.DayOfWeek - 1;
            if (days < 0) days += 7;
            var weekBeginning = date.Subtract(new TimeSpan(days, 0, 0, 0));

            var shoppingList = _shoppingListService.GetShoppingList(userId, weekBeginning)
                .Select(_shoppingListService.ToDto)
                .ToList();

            return Json(new SuccessResponse<object>(new
            {
                shoppingList
            }));
        }

        [Route("{userId:int}/items"), HttpPost]
        [Route("{userId:int}/{dateString:datetime}/items"), HttpPost]
        public IActionResult AddItem([FromBody] ShoppingListItemDto request, int userId, string dateString)
        {
            var claims = HttpContext.User.Claims.ToList();
            int authorizedId = 0;
            bool isAdmin = false;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out authorizedId);
            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);

            if (userId != authorizedId && !isAdmin)
            {
                Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return Json(new ErrorResponse("Not authorized to make this request", (int) HttpStatusCode.Forbidden));
            }

            DateTime date;
            if (dateString == "")
            {
                date = DateTime.Now.Date;
            }
            else
            {
                var dateParts = dateString.Split('-');
                if (dateParts.Length != 3)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-mm-dd)", (int)HttpStatusCode.BadRequest));
                }

                int year, month, day;
                if (!int.TryParse(dateParts[0], out year)
                    || !int.TryParse(dateParts[1], out month)
                    || !int.TryParse(dateParts[2], out day))
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new ErrorResponse("Invalid date passed. Make sure it is in format (YYYY-mm-dd)", (int)HttpStatusCode.BadRequest));
                }
                date = new DateTime(year, month, day);
            }

            var days = (int)date.DayOfWeek - 1;
            if (days < 0) days += 7;
            var weekBeginning = date.Subtract(new TimeSpan(days, 0, 0, 0));


            var item = _shoppingListService.AddItem(userId, weekBeginning, request);

            if (item != null)
            {
                return Json(new SuccessResponse<object>(new
                {
                    item =_shoppingListService.ToDto(item)
                }));
            }
            else
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return Json(new ErrorResponse("Failed to add item", (int) HttpStatusCode.BadRequest));
            }
        }

        [Route("me/items/{id:int}")]
        [Route("{userId:int}/items/{id:int}")]
        [HttpPut]
        public IActionResult UpdateItem([FromBody]ShoppingListItemDto request, int userId, int id)
        {
            var claims = HttpContext.User.Claims.ToList();
            int authorizedId = 0;
            bool isAdmin = false;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out authorizedId);
            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);

            if (userId == 0)
            {
                userId = authorizedId;
            }
            else if (userId != authorizedId && !isAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Json(new ErrorResponse("Not authorized to make this request", (int)HttpStatusCode.Forbidden));
            }

            if (request.Id != id)
            {
                request.Id = id;
            }

            var response = _shoppingListService.UpdateItem(userId, request);

            if (response)
            {
                return Json(new SuccessResponse());
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new ErrorResponse("Failed to add item", (int)HttpStatusCode.BadRequest));
            }
        }

        [Route("me/items/{id:int}"), HttpDelete]
        [Route("{userId:int}/items/{id:int}"), HttpDelete]
        public IActionResult RemoveItem(int userId, int id)
        {
            var claims = HttpContext.User.Claims.ToList();
            int authorizedId = 0;
            bool isAdmin = false;

            Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out authorizedId);
            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);

            if (userId == 0)
            {
                userId = authorizedId;
            }
            else if (userId != authorizedId && !isAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Json(new ErrorResponse("Not authorized to make this request", (int)HttpStatusCode.Forbidden));
            }

            var response = _shoppingListService.RemoveItem(userId, id);

            if (response)
            {
                return Json(new SuccessResponse());
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new ErrorResponse("Failed to add item", (int)HttpStatusCode.BadRequest));
            }
        }
    }
}
