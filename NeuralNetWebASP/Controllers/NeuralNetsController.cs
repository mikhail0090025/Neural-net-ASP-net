﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neural_net;
using NeuralNetWebASP.Models;

namespace NeuralNetWebASP.Controllers
{
    [Route("api/[controller]")]
    public class NeuralNetsController : Controller
    {
        static Generation Generation { get; set; }
        static LearningDatabase LearningDatabase { get; set; }
        static GenerationData GenerationData { get; set; }
        static bool IsLearning { get; set; }
        ILogger<NeuralNetsController> Logger { get; set; }
        public NeuralNetsController(ILogger<NeuralNetsController> _logger)
        {
            Logger = _logger;
            IsLearning = false;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost("newgeneration")]
        public IActionResult CreateNewGeneration([FromForm] GenerationParameters parameters)
        {
			try
			{
                parameters.learning_factor = Math.Pow(parameters.learning_factor, -1);
                Generation = new Generation(parameters.inputs_count, parameters.outputs_count, parameters.hidden_layers_count, parameters.neurals_in_hidden_layer_count, parameters.gen_size, (RoundType)parameters.inp_round, (RoundType)parameters.neu_round, (RoundType)parameters.out_round, parameters.learning_factor);
                LearningDatabase = new LearningDatabase(parameters.inputs_count, parameters.outputs_count);
                Generation.SetDatabase(LearningDatabase);
			}
            catch(Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
            return Ok("Generation was created!");
        }
        [HttpPost("addtodb")]
        public IActionResult AddElementToDB([FromBody] DataDB data)
        {
            try
            {
                LearningDatabase.AddItem(data.LearningInputs, data.ExpectedOutputs);
            }
            catch (Exception ex)
            {
                return BadRequest($"<span style='color: red;'>Error: {ex.Message}</span>");
            }
            return Ok("<span style='color: green;'>Example was successfully added!</span>");
        }
        private GenerationParameters GenerationParameters()
        {
            if(Generation == null)
            {
                throw new Exception("Generation is null");
            }
			GenerationParameters parameters = new GenerationParameters();
			parameters.inputs_count = Generation.InputsCount;
			parameters.outputs_count = Generation.OutputsCount;
			parameters.neurals_in_hidden_layer_count = Generation.NeuralsInHiddenLayersAmount;
			parameters.hidden_layers_count = Generation.HiddenLayersCount;
			parameters.gen_size = Generation.Size;
			parameters.inp_round = (int)Generation.InputsRound;
			parameters.out_round = (int)Generation.OutputsRound;
			parameters.neu_round = (int)Generation.NeuralsRound;
			parameters.learning_factor = (int)Generation.LearningFactor;
            return parameters;
		}
        [HttpGet("GetAddingToDBview")]
        public IActionResult GetAddingToDBview()
        {
            if (Generation == null)
            {
                return BadRequest("<p style='color: red;'><b>Generation was not created! Create it firstly.</b></p>");
            }
			var parameters = GenerationParameters();
            Logger.LogInformation($"Parameters are null: {parameters == null}");
            return View("AddToDBView", parameters);
        }
        [HttpPost("passgeneration")]
        public async Task<IActionResult> PassOneGeneration()
        {
            if (Generation == null)
            {
                return BadRequest("Generation was not created!");
            }
            try
            {
                await Generation.PassOneGenerationAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            GenerationData = new GenerationData(LearningDatabase.Size, Generation.GenerationsPassed, Generation.CurrentError, Generation.ErrorChange, Generation.LearningFactor);
            return Json(GenerationData);
        }
        [HttpPost("passseveralgenerations")]
        public async Task<IActionResult> PassSeveralGenerations([FromBody] int amount)
        {
            var current_error = Generation.CurrentError;
            if (Generation == null)
            {
                return BadRequest("Generation was not created!");
            }
            try
            {
                for (int i = 0; i < amount; i++)
                {
                    await Generation.PassOneGenerationAsync();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            GenerationData = new GenerationData(LearningDatabase.Size, Generation.GenerationsPassed, Generation.CurrentError, current_error - Generation.CurrentError, Generation.LearningFactor);
            return Json(GenerationData);
        }
        [HttpGet("generationisnull")]
        public IActionResult GenerationIsNull()
        {
            return Json(new {isnull = Generation == null});
        }
        [HttpPost("changelearningfactor")]
        public IActionResult ChangeLearningFactor([FromBody] bool increase)
        {
            try
            {
                if (increase)
                {
                    Generation.IncreaseLearningFactor();
                }
                else
                {
                    Generation.ReduceLearningFactor();
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error while changing learning factor: {ex.Message}");
            }
            return Ok($"Learning factor is changed to {Generation.LearningFactor}");
        }
        [HttpGet("gendata")]
        public IActionResult GenData()
        {
            return Json(GenerationData);
        }
        static double cur_error;
        [HttpPost("start_learning")]
        public async Task<IActionResult> StartLearning()
        {
            cur_error = Generation.CurrentError;
            IsLearning = true;
            while (IsLearning)
            {
                await Generation.PassOneGenerationAsync();
                Logger.LogInformation("Iteration passed!");
            }
            return Ok("Learning step is finished");
        }
        [HttpPost("stop_learning")]
        public IActionResult StopLearning()
        {
            IsLearning = false;
            GenerationData = new GenerationData(LearningDatabase.Size, Generation.GenerationsPassed, Generation.CurrentError, cur_error - Generation.CurrentError, Generation.LearningFactor);
            return Ok("Learning is stopped");
        }
        [HttpGet("get_view_with_test_nn")]
        public IActionResult ViewWithTestNN()
        {

        }
    }
}
public class DataDB
{
    public List<double> LearningInputs { get; set; }
    public List<double> ExpectedOutputs { get; set; }
}