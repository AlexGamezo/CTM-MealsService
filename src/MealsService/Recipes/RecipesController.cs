using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MealsService.Requests;
using MealsService.Responses;
using MealsService.Recipes.Dtos;
using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Configurations;
using MealsService.Images;

namespace MealsService.Recipes
{
    [Route("[controller]")]
    public class RecipesController : AuthorizedController
    {
        private IRecipesService _recipesService;
        private IImageService _imageService;
        private IUserRecipesService _userRecipesService;
        private AWSConfiguration _awsOptions;

        public RecipesController(IRecipesService recipesService, IUserRecipesService userRecipesService, IImageService imageService, IOptions<AWSConfiguration> options)
        {
            _recipesService = recipesService;
            _userRecipesService = userRecipesService;
            _imageService = imageService;
            _awsOptions = options.Value;
        }

        [HttpGet]
        public async Task<IActionResult> ListAsync([FromQuery] RecipeListRequest request)
        {
            if (request.UserId > 0 && (AuthorizedUser != request.UserId || !IsAdmin))
            {
                throw StandardErrors.ForbiddenRequest;
            }

            if (request.UserId == 0)
            {
                request.UserId = AuthorizedUser;
            }

            if (!IsAdmin && request.IncludeDeleted)
            {
                request.IncludeDeleted = false;
            }

            var recipes = _recipesService.ListRecipes(request);

            if (request.UserId > 0)
            {
                await _userRecipesService.PopulateRecipeVotesAsync(recipes, request.UserId);
            }
            
            return Json(new SuccessResponse<object>(new 
            {
                recipes
            }));
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchAsync([FromQuery] RecipeSearchRequest request)
        {
            if (!IsAdmin && request.IncludeDeleted)
            {
                request.IncludeDeleted = false;
            }

            var recipes = _recipesService.SearchRecipes(request);

            if (AuthorizedUser > 0)
            {
                await _userRecipesService.PopulateRecipeVotesAsync(recipes, AuthorizedUser);
            }

            return Json(new SuccessResponse<object>(new
            {
                recipes
            }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var recipe = _recipesService.GetRecipe(id);

            if (AuthorizedUser > 0)
            {
                await _userRecipesService.PopulateRecipeVotesAsync(new List<RecipeDto> {recipe}, AuthorizedUser);
            }

            if(recipe != null)
            {
                return Json(new SuccessResponse<RecipeDto>(recipe));
            }
            else
            {
                Response.StatusCode = 404;
                return Json(new ErrorResponse("Could not find the recipe", 404));
            }
        }

        [HttpGet("{slug:regex(^[[A-Za-z0-9\\-]]+$)}")]
        public async Task<IActionResult> GetAsync(string slug)
        {
            var recipe = _recipesService.GetRecipeBySlug(slug);

            if (AuthorizedUser > 0)
            {
                await _userRecipesService.PopulateRecipeVotesAsync(new List<RecipeDto> { recipe }, AuthorizedUser);
            }

            if (recipe != null)
            {
                return Json(new SuccessResponse<RecipeDto>(recipe));
            }
            else
            {
                Response.StatusCode = 404;
                return Json(new ErrorResponse("Could not find the recipe", 404));
            }
        }

        [Authorize]
        [HttpPost("{id:int}/votes")]
        public async Task<IActionResult> VoteAsync(int id, [FromBody] RecipeVoteDto request)
        {
            var success = await _userRecipesService.AddRecipeVoteAsync(AuthorizedUser, id, request.Vote);
            var response = new RecipeVoteResponse
            {
                RecipeId = id, Vote = request.Vote
            };

            return Json(response);
        }

        [Authorize]
        [HttpGet("votes/me")]
        [HttpGet("votes/{userId:int}")]
        public async Task<IActionResult> ListVotesAsync(int userId)
        {
            if (userId == 0)
            {
                userId = AuthorizedUser;
            }
            else if(userId != AuthorizedUser && !IsAdmin)
            {
                throw StandardErrors.ForbiddenRequest;
            }

            var votes = await _userRecipesService.ListRecipeVotesAsync(userId);

            return Json(new SuccessResponse<object>(new
            {
                votes
            }));
        }

        [Authorize, AdminRequiredFilter]
        [HttpPost]
        public IActionResult Create([FromBody]RecipeDto recipe)
        {
            var updatedRecipe = _recipesService.SaveRecipe(recipe);

            return Json(new SuccessResponse<RecipeDto>(updatedRecipe));
        }


        [Authorize, AdminRequiredFilter]
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody]RecipeDto recipe)
        {
            recipe.Id = id;
            var updatedRecipe = _recipesService.SaveRecipe(recipe);

            return Json(new SuccessResponse<RecipeDto>(updatedRecipe));
        }

        [Authorize, AdminRequiredFilter]
        [HttpPost("{recipeId:int}/image")]
        public async Task<IActionResult> UploadImageAsync(int recipeId)
        {
            var foundRecipe = _recipesService.GetRecipe(recipeId);

            if (foundRecipe == null)
            {
                throw StandardErrors.MissingRequestedItem;
            }

            var recipeImageFile = HttpContext.Request.Form.Files.FirstOrDefault();
            var extension = recipeImageFile.ContentType.Substring(recipeImageFile.ContentType.IndexOf("/") + 1);
            
            var imagePath = await _imageService.UploadImageAsync(recipeId.ToString() + "." + extension, _awsOptions.RecipeImagesBucket, recipeImageFile);

            foundRecipe.Image = imagePath;
            _recipesService.SaveRecipe(foundRecipe);
            
            return Json(new SuccessResponse());
        }


        [Authorize, AdminRequiredFilter]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _recipesService.DeleteRecipe(id);

            return Json(new SuccessResponse(success));
        }
    }
}
