using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FluentAssertions;
using MealsService.Common.Errors;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using UnitsNet;
using UnitsNet.Units;

namespace MealsService.Tests.Ingredients
{
    public class IngredientMeasurementsTests
    {
        private IngredientsService _ingredientsService;

        private IMemoryCache _memoryCache;
        private IIngredientsRepository _fakeRepository;

        [SetUp]
        public void Setup()
        {
            _memoryCache = A.Fake<IMemoryCache>();
            _fakeRepository = GetIngredientsRepository();

            _ingredientsService = new IngredientsService(_memoryCache, _fakeRepository);
        }

        private IIngredientsRepository GetIngredientsRepository()
        {
            var fakeRepository = A.Fake<IIngredientsRepository>();

            var ingredients = new Dictionary<int, Ingredient>
            {
                {1, new Ingredient{ Id = 1, Name = "Volume Ingredient 1", IsMeasuredVolume = true } },
                {2, new Ingredient{ Id = 2, Name = "Volume Ingredient 2", IsMeasuredVolume = true } },
                {3, new Ingredient{ Id = 3, Name = "Mass Ingredient 1", IsMeasuredVolume = false } },
                {4, new Ingredient{ Id = 4, Name = "Mass Ingredient 2", IsMeasuredVolume = false } }
            };
            var ingredientCategories = new Dictionary<int, IngredientCategory>
            {
                {1, new IngredientCategory{ Id = 1, Name = "category 1", Order = 1}},
                {2, new IngredientCategory{ Id = 2, Name = "category 2", Order = 2}}
            };

            
            A.CallTo(() => fakeRepository.ListIngredients())
                .ReturnsLazily(() => ingredients.Values.ToList());

            A.CallTo(() => fakeRepository.ListIngredientCategories())
                .ReturnsLazily(() => ingredientCategories.Values.ToList());

            A.CallTo(() => fakeRepository.DeleteIngredientById(A<int>.Ignored))
                .ReturnsLazily((int id) => ingredients.Remove(id));
            A.CallTo(() => fakeRepository.DeleteIngredientCategoryById(A<int>.Ignored))
                .ReturnsLazily((int id) => ingredientCategories.Remove(id));

            return fakeRepository;
        }

        [Test]
        public void NormalizeVolumeIngredientTest()
        {
            var volumeIngredient = new MeasuredIngredient
            {
                IngredientId = 1,
                Quantity = 0.6
            };

            _ingredientsService.NormalizeMeasuredIngredient(volumeIngredient);

            volumeIngredient.Measure.Should().NotBeNullOrEmpty();
            volumeIngredient.Measure.Should().Be("tbsp");
            volumeIngredient.Quantity.Should().Be(1.25);

            volumeIngredient = new MeasuredIngredient
            {
                IngredientId = 1,
                Quantity = 0.1
            };

            _ingredientsService.NormalizeMeasuredIngredient(volumeIngredient);

            volumeIngredient.Measure.Should().NotBeNullOrEmpty();
            volumeIngredient.Measure.Should().Be("tsp");
            volumeIngredient.Quantity.Should().Be(0.5);

            volumeIngredient = new MeasuredIngredient
            {
                IngredientId = 1,
                Quantity = 2.7
            };

            _ingredientsService.NormalizeMeasuredIngredient(volumeIngredient);

            volumeIngredient.Measure.Should().NotBeNullOrEmpty();
            volumeIngredient.Measure.Should().Be("cups");
            volumeIngredient.Quantity.Should().BeApproximately(0.333, 0.001);

            volumeIngredient = new MeasuredIngredient
            {
                IngredientId = 1,
                Quantity = 3.4
            };

            _ingredientsService.NormalizeMeasuredIngredient(volumeIngredient);

            volumeIngredient.Measure.Should().NotBeNullOrEmpty();
            volumeIngredient.Measure.Should().Be("cups");
            volumeIngredient.Quantity.Should().BeApproximately(0.5, 0.001);
        }

        [Test]
        public void GroupIngredientsTest()
        {

        }
    }
}
