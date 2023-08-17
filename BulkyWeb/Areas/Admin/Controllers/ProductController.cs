using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment? webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            return View(objProductList);
        }


        public IActionResult Upsert(int? id) 
        {
            ProductViewModels productViewModels = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),

                Product = new Product()
            };
            if (id==null || id==0)
            {
                //create
                return View(productViewModels);
            }
            else
            {
                //update
                productViewModels.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productViewModels);
            }

        }


        [HttpPost]
        public IActionResult Upsert(ProductViewModels productViewModels, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPAth = _webHostEnvironment.WebRootPath;
                
                if (file != null)
                {

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath =Path.Combine(wwwRootPAth, @"images\product");

                    if (!string.IsNullOrEmpty(productViewModels.Product.ImageUrl))
                    {
                        //delete the old image
                        var oldImagePath=Path.Combine(wwwRootPAth,productViewModels.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName),FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                        fileStream.Position = 0;
                    }

                    productViewModels.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if (productViewModels.Product.Id > 0 || productViewModels.Product.Id == null)
                {
                    _unitOfWork.Product.Update(productViewModels.Product);
                }
                else
                {
                    _unitOfWork.Product.Add(productViewModels.Product);
                }


                //_unitOfWork.Product.Update(productViewModels.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product updated successfully";
                return RedirectToAction("Index");
            }

            else
            {
                productViewModels.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                
                return View(productViewModels);
            }
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(int id)
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new {data = objProductList});

        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u=>u.Id==id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();
            
            return Json(new { success = true, message = "Deleted successful" });

        }
        #endregion

    }
}
