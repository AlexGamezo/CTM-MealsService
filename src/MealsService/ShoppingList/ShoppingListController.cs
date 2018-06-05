using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
using MealsService.Infrastructure;
using MealsService.Responses;
using MealsService.ShoppingList.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;

namespace MealsService.ShoppingList
{
    [Route("[controller]")]
    public class ShoppingListController : Controller
    {
        private ShoppingListService _shoppingListService { get; }
        private IServiceProvider _serviceProvider { get; }

        public ShoppingListController(ShoppingListService shoppingListService, IServiceProvider serviceProvider)
        {
            _shoppingListService = shoppingListService;
            _serviceProvider = serviceProvider;
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

            var result = LocalDatePattern.Iso.Parse(dateString);
            LocalDate localDate;
            if (result.Success)
            {
                localDate = result.Value;
            }
            else
            {
                throw StandardErrors.InvalidDateSpecified;
            }

            var shoppingList = _shoppingListService.GetShoppingList(userId, localDate.GetWeekStart())
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

            var result = LocalDatePattern.Iso.Parse(dateString);
            LocalDate localDate;
            if (result.Success)
            {
                localDate = result.Value;
            }
            else
            {
                throw StandardErrors.InvalidDateSpecified;
            }

            var item = _shoppingListService.AddItem(userId, localDate.GetWeekStart(), request);

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
