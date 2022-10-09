using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesWebApp.Models;
using MoviesWebApp.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MoviesWebApp.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        private int _maxAllowedPosterSize = 1048576;
        private new List<string> _allowedExtenstions = new() { ".jpg", ".png" };
        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        public ApplicationDbContext Context { get; }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Movie> movies = await _context.Movies.OrderByDescending(m => m.Rate).ToListAsync();
            return View(movies);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Create";
            MovieViewModel movie = new MovieViewModel
            {
                Genres =await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", movie);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieViewModel model)
        {
            ViewData["Title"] = "Create";
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var files = Request.Form.Files;

            if (!files.Any())
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please select movie poster!");
                return View("MovieForm", model);
            }

            var poster = files.FirstOrDefault();

            if (!ckeckFilesExtentions(poster))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                return View("MovieForm", model);
            }

            if (poster.Length > _maxAllowedPosterSize)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                return View("MovieForm", model);
            }
            using var dataStream = new MemoryStream();
            await poster.CopyToAsync(dataStream);
            Movie movie = new Movie()
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                Storeline = model.Storeline,
                Poster = dataStream.ToArray()
            };

            //var config = new MapperConfiguration(cfg =>
            //        cfg.CreateMap<Movie, model>()
            //    );
            _context.Movies.Add(movie);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        public async  Task<IActionResult> Edit(int? id)
        {
            ViewData["Title"] = "Edit";
            if (id == null)
                return BadRequest();
            Movie movie = await _context.Movies.FindAsync(id);
            if(movie == null)
                return NotFound();
            MovieViewModel model = new MovieViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                GenreId = movie.GenreId,
                Year = movie.Year,
                Rate = movie.Rate,
                Storeline = movie.Storeline,
                Poster = movie.Poster,
                Genres = _context.Genres.OrderBy(m => m.Name),

            };
            return View("MovieForm", model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async  Task<IActionResult> Edit(MovieViewModel model)
        {
            ViewData["Title"] = "Edit";
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
            Movie movie =await _context.Movies.FindAsync(model.Id);

            if (movie == null)
                return NotFound();

            IFormFileCollection files = Request.Form.Files;
            if (files.Any())
            {
                IFormFile poster = files.FirstOrDefault();
                using var dataStream = new MemoryStream();
                await poster.CopyToAsync(dataStream);
                model.Poster = dataStream.ToArray();

                if(!ckeckFilesExtentions(poster))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                    return View("MovieForm", model);
                }
                if (!ckeckFileLengthIsAllowed(poster))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                    return View("MovieForm", model);
                }
                movie.Poster = model.Poster;

            }
            movie.Title = model.Title;
            movie.Storeline = model.Storeline;
            movie.Rate = model.Rate;
            movie.Year = model.Year;
            movie.GenreId = model.GenreId;
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();

            Movie movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            int genreId = movie.GenreId;
            Genre x = await _context.Genres.FindAsync(genreId);
            ViewBag.Genre = x.Name;
            return View(movie);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            Movie movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();
            _context.Movies.Remove(movie);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        public bool ckeckFilesExtentions(IFormFile file)
        {
            return (_allowedExtenstions.Contains(Path.GetExtension(file.FileName).ToLower()));
        }
        
        public bool ckeckFileLengthIsAllowed(IFormFile file)
        {
            return (file.Length <= _maxAllowedPosterSize);
        }

    }
        
    
}

