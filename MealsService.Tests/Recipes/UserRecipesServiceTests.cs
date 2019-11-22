using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching;

using FakeItEasy;
using FluentAssertions;
using MealsService.Common;
using MealsService.Common.Errors;
using NUnit.Framework;

using MealsService.Ingredients;
using MealsService.Recipes;
using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Users;

namespace MealsService.Tests.Recipes
{
    public class UserRecipesServiceTests
    {
        private IUserRecipesService _userRecipesService;
        private IUserRecipeRepository _userRecipeRepo;

        private IRecipesService _recipesService;
        private IIngredientsService _ingredientsService;
        private IMemcachedClient _fakeCache;

        [SetUp]
        public void Setup()
        {
            _fakeCache = A.Fake<IMemcachedClient>();
            _userRecipeRepo = GetFakeRecipeRepository();
            _ingredientsService = GetFakeIngredientsService();
            _recipesService = GetFakeRecipesService();

            A.CallTo(() =>
                    _fakeCache.GetValueOrCreateAsync(A<string>.Ignored, A<int>.Ignored,
                        A<Func<Task<List<RecipeVoteDto>>>>.Ignored))
                .ReturnsLazily((string key, int duration, Func<Task<List<RecipeVoteDto>>> generator) =>
                {
                    return generator.Invoke();
                });
            A.CallTo(() =>
                    _fakeCache.GetValueOrCreateAsync(A<string>.Ignored, A<int>.Ignored,
                        A<Func<Task<List<int>>>>.Ignored))
                .ReturnsLazily((string key, int duration, Func<Task<List<int>>> generator) =>
                {
                    return generator.Invoke();
                });
            A.CallTo(() => _fakeCache.SetAsync(A<string>.Ignored, A<object>.Ignored, A<int>.Ignored))
                .Returns(true);

            var fakeUsersService = A.Fake<UsersService>();
            A.CallTo(() => fakeUsersService.UpdateJourneyProgressAsync(A<int>.Ignored, A<Users.Data.UpdateJourneyProgressRequest>.Ignored))
                .Returns(Task.FromResult(true));

            _userRecipesService = new UserRecipesService(_ingredientsService, _recipesService, _userRecipeRepo, _fakeCache, fakeUsersService);
        }

        private IRecipesService GetFakeRecipesService()
        {
            var recipes = new List<RecipeDto>
            {
                new RecipeDto{ Id = 1, Name = "Recipe 1", Ingredients = new List<RecipeIngredientDto>(), DietTypes = new List<int>{ 1 }, MealType = MealType.Dinner},
                new RecipeDto{ Id = 2, Name = "Recipe 2", Ingredients = new List<RecipeIngredientDto>(), DietTypes = new List<int>{ 1 }, MealType = MealType.Dinner },
                new RecipeDto{ Id = 3, Name = "Recipe 3", Ingredients = new List<RecipeIngredientDto>(), DietTypes = new List<int>{ 1 }, MealType = MealType.Dinner },
                new RecipeDto{ Id = 4, Name = "Recipe 4", Ingredients = new List<RecipeIngredientDto>(), DietTypes = new List<int>{ 1 }, MealType = MealType.Dinner },
            };

            var service = A.Fake<IRecipesService>();

            A.CallTo(() => service.ListRecipes(A<RecipeListRequest>.Ignored))
                .ReturnsLazily((RecipeListRequest request) =>
                {
                    return recipes;
                });
            A.CallTo(() => service.SearchRecipes(A<RecipeSearchRequest>.Ignored))
                .ReturnsLazily((RecipeSearchRequest request) =>
                {
                    return recipes.Where(r => r.MealType == request.MealType).ToList();
                });

            return service;
        }

        private IIngredientsService GetFakeIngredientsService()
        {
            var service = A.Fake<IIngredientsService>();

            return service;
        }

        private IUserRecipeRepository GetFakeRecipeRepository()
        {
            var userVotes = new Dictionary<int, List<RecipeVote>>();
            var repo = A.Fake<IUserRecipeRepository>();

            A.CallTo(() => repo.GetUserVotes(A<int>.Ignored))
                .ReturnsLazily((int userId) =>
                {
                    if (!userVotes.ContainsKey(userId))
                    {
                        userVotes.Add(userId, new List<RecipeVote>());
                    }
                    return userVotes[userId];
                });
            A.CallTo(() => repo.SaveVote(A<RecipeVote>.Ignored))
                .ReturnsLazily((RecipeVote vote) =>
                {
                    var detachedVote = new RecipeVote { Id = vote.Id, UserId = vote.UserId, RecipeId = vote.RecipeId, Vote = vote.Vote };
                    if (detachedVote.UserId == 0 || detachedVote.RecipeId == 0)
                    {
                        throw StandardErrors.CouldNotCreateEntity;
                    }

                    if(!userVotes.ContainsKey(detachedVote.UserId))
                    {
                        userVotes.Add(detachedVote.UserId, new List<RecipeVote>());
                    }

                    var existingVote = userVotes[detachedVote.UserId].FirstOrDefault(v => v.RecipeId == detachedVote.RecipeId);
                    if (existingVote != null)
                    {
                        existingVote.Vote = detachedVote.Vote;
                    }
                    else
                    {
                        if (detachedVote.Id == 0)
                        {
                            detachedVote.Id = detachedVote.UserId * 100 + userVotes[detachedVote.UserId].Count + 1;
                        }
                        userVotes[detachedVote.UserId].Add(detachedVote);
                    }

                    return true;
                });
            A.CallTo(() => repo.GetRecipeVotes(A<int>.Ignored))
                .ReturnsLazily((int recipeId) =>
                {
                    return userVotes.SelectMany(uv => uv.Value.Where(v => v.RecipeId == recipeId).ToList())
                        .ToList();
                });

            return repo;
        }

        private void AddFakeVotes()
        {
            _userRecipeRepo.SaveVote(new RecipeVote {RecipeId = 1, UserId = 1, Vote = RecipeVote.VoteType.LIKE});
            _userRecipeRepo.SaveVote(new RecipeVote {RecipeId = 2, UserId = 1, Vote = RecipeVote.VoteType.LIKE});
            _userRecipeRepo.SaveVote(new RecipeVote {RecipeId = 1, UserId = 2, Vote = RecipeVote.VoteType.HATE});
            _userRecipeRepo.SaveVote(new RecipeVote {RecipeId = 1, UserId = 3, Vote = RecipeVote.VoteType.LIKE});
            _userRecipeRepo.SaveVote(new RecipeVote {RecipeId = 2, UserId = 3, Vote = RecipeVote.VoteType.UNKNOWN});
        }

        [Test]
        public async Task ListRecipeVotesTestAsync()
        {
            var testUserId = 1;
            
            var recipeVotes = await _userRecipesService.ListRecipeVotesAsync(testUserId);
            recipeVotes.Should().NotBeNull();
            recipeVotes.Should().BeEmpty();

            AddFakeVotes();

            recipeVotes = await _userRecipesService.ListRecipeVotesAsync(testUserId);
            recipeVotes.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task AddNewVoteTestAsync()
        {
            var testUserId = 1;
            var testRecipeId = 1;

            var status = await _userRecipesService.AddRecipeVoteAsync(testUserId, testRecipeId, RecipeVote.VoteType.LIKE);
            status.Should().BeTrue();

            A.CallTo(() => _userRecipeRepo.GetUserVotes(testUserId))
                .MustHaveHappened(1, Times.Exactly).Then(
            A.CallTo(() => _userRecipeRepo.SaveVote(A<RecipeVote>.Ignored))
                .MustHaveHappened(1, Times.Exactly));

            A.CallTo(() => _fakeCache.Remove(CacheKeys.Recipes.UserVotes(testUserId)))
                .MustHaveHappened(1, Times.Exactly);
        }

        [Test]
        public async Task UpdateVoteTest()
        {
            var testUserId = 1;
            var testRecipeId = 1;

            AddFakeVotes();
            
            var votes = await _userRecipesService.ListRecipeVotesAsync(testUserId);
            votes.Should().NotBeNullOrEmpty();
            var recipeVote = votes.FirstOrDefault(v => v.RecipeId == testRecipeId);
            recipeVote.Should().NotBeNull();
            recipeVote.Vote.Should().Be(RecipeVote.VoteType.LIKE);

            var status = await _userRecipesService.AddRecipeVoteAsync(testUserId, testRecipeId, RecipeVote.VoteType.HATE);
            status.Should().BeTrue();

            A.CallTo(() => _userRecipeRepo.GetUserVotes(testUserId))
                .MustHaveHappened(2, Times.Exactly).Then(
            A.CallTo(() => _userRecipeRepo.SaveVote(A<RecipeVote>.Ignored))
                .WhenArgumentsMatch((RecipeVote vote) => vote.Id != 0 && vote.RecipeId == testRecipeId && vote.UserId == testUserId)
                .MustHaveHappenedOnceExactly());

            A.CallTo(() => _fakeCache.Remove(CacheKeys.Recipes.UserVotes(testUserId)))
                .MustHaveHappened(1, Times.Exactly);

            votes = await _userRecipesService.ListRecipeVotesAsync(testUserId);
            votes.Should().NotBeNullOrEmpty();
            recipeVote = votes.FirstOrDefault(v => v.RecipeId == testRecipeId);
            recipeVote.Should().NotBeNull();
            recipeVote.Vote.Should().Be(RecipeVote.VoteType.HATE);
        }

        [Test]
        public async Task PopulateRecipeVotesTest()
        {
            var testUserId = 1;
            var recipesList = _recipesService.ListRecipes();

            AddFakeVotes();

            await _userRecipesService.PopulateRecipeVotesAsync(recipesList, testUserId);

            A.CallTo(() => _userRecipeRepo.GetUserVotes(testUserId))
                .MustHaveHappened(1, Times.Exactly);

            var recipe = recipesList.FirstOrDefault(r => r.Id == 1);
            recipe.Should().NotBeNull();
            recipe.Id.Should().Be(1);
            recipe.Vote.Should().NotBe(RecipeVote.VoteType.UNKNOWN);
        }

        [Test]
        public async Task GetRandomRecipeAsyncTest()
        {
            var testUserId = 1;
            var request = new RandomRecipeRequest()
            {
                DietTypeId = 1,
                MealType = MealType.Dinner
            };

            var recipe = await _userRecipesService.GetRandomRecipeAsync(request, testUserId);
            recipe.Should().NotBeNull();
            recipe.DietTypes.Should().Contain(request.DietTypeId);
            recipe.MealType.Should().Be(request.MealType);

            A.CallTo(() => _recipesService.SearchRecipes(A<RecipeSearchRequest>.Ignored))
                .MustHaveHappenedOnceExactly();

        }
    }
}
