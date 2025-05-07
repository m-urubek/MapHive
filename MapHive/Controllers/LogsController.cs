using AutoMapper;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly IDataGridRepository _dataGridRepository;
        private readonly IMapper _mapper;

        public LogsController(IDataGridRepository dataGridRepository, IMapper mapper)
        {
            this._dataGridRepository = dataGridRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int page = 1,
            int pageSize = 20,
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            // Prepare empty grid: fetch only column definitions
            List<DataGridColumnGet> columns = await this._dataGridRepository.GetColumnsForTableAsync("Logs");
            DataGridViewModel viewModel = new()
            {
                TableName = "Logs",
                Columns = columns,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortDirection = sortDirection,
                CurrentPage = page,
                PageSize = pageSize
            };
            return this.View(viewModel);
        }
    }
}