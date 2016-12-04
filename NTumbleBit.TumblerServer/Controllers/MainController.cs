﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NTumbleBit.PuzzlePromise;
using NTumbleBit.PuzzleSolver;
using NTumbleBit.ClassicTumbler;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace NTumbleBit.TumblerServer.Controllers
{
	public class MainController : Controller
	{
		public MainController(TumblerConfiguration configuration, ClassicTumblerParameters parameters)
		{
			if(configuration == null)
				throw new ArgumentNullException("configuration");
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			_Tumbler = configuration;
			Repository = configuration.Repository;
			Parameters = parameters;
		}

		public ClassicTumblerRepository Repository
		{
			get; set;
		}

		private readonly TumblerConfiguration _Tumbler;
		public TumblerConfiguration Tumbler
		{
			get
			{
				return _Tumbler;
			}
		}

		public ClassicTumblerParameters Parameters
		{
			get;
			private set;
		}

		[HttpGet("api/v1/tumblers/0/parameters")]
		public ClassicTumblerParameters GetSolverParameters()
		{
			return Parameters;
		}

		[HttpGet("api/v1/tumblers/0/askvoucher")]
		public PuzzleValue AskVoucherParameters()
		{
			var height = Repository.GetCurrentBlockHeight();

			var cycleParameters = Tumbler.CycleGenerator.GetRegistratingCycle(height);
			var bobSession = new TumblerBobServerSession(Parameters, Tumbler.TumblerKey, Tumbler.VoucherKey, cycleParameters);
			var voucher = bobSession.GenerateUnsignedVoucher();
			Repository.Save(bobSession);
			return voucher;
		}

	}
}
