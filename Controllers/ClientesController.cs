﻿using Front_End_Gestion_Pedidos.Helpers;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ClientesController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public ClientesController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }
             
        public async Task<IActionResult> Index()
        {
            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");

            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            var viewModel = new ClienteViewModel();
            // Recuperar clientes desde la API
            viewModel.Clientes = (await ObtenerClientes())?.ToList() ?? new List<Cliente>();

            return View(viewModel); // Pasar la lista de clientes a la vista
        }

        public async Task<IActionResult> Clientes()
        {
            var model = new PedidoViewModel();
            // Recuperar datos de la sesión y asignarlos al ViewBag
            //string? token = HttpContext.Session.GetString("token");

            // Verifica si la sesión está activa
            //if (token == null)
            //{
            //    return RedirectToAction("login", "account"); // redirige al login si no hay sesión activa
            //}

            var clientes = await ObtenerClientes();
            if (clientes != null)
            {
                model.Clientes = clientes.ToList();
            }
            else
            {
                model.Clientes = new List<Cliente>();
            }

            return View(model);
        }

        private async Task<IEnumerable<Cliente>> ObtenerClientes()
        {
            var model = new PedidoViewModel();
            List<Cliente> clientes1 = new List<Cliente>();

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/v1/Clientes");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                model.Clientes = clientes ?? new List<Cliente>();

                clientes1 = model.Clientes;
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener los clientes.");
                return null;
            }

            return clientes1;
        }


        [HttpPost]
        public async Task<IActionResult> VerDatosContacto(long clienteId)
        {
            var viewModel = new ClienteViewModel();

            try
            {
                // Obtener los datos de contacto del cliente
                viewModel.DatosContacto = (await ObtenerDatosContactoPorCliente(clienteId))?.ToList() ?? new List<DatosContacto>();

                // Obtener todos los clientes para la tabla
                viewModel.Clientes = (await ObtenerClientes())?.ToList() ?? new List<Cliente>();
            }
            catch (Exception ex)
            {
                // Manejar el error y retornar un mensaje adecuado
                viewModel.DatosContacto = new List<DatosContacto>();
                viewModel.Clientes = new List<Cliente>();
                viewModel.Error = $"Error al obtener los datos: {ex.Message}";
            }

            return View("Index", viewModel);
        }


        private async Task<IEnumerable<DatosContacto>> ObtenerDatosContactoPorCliente(long clienteId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/v1/Clientes/{clienteId}/DatosContacto");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<DatosContacto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                // Manejar errores en la solicitud a la API
                throw new Exception($"Error al obtener los datos de contacto para el cliente {clienteId}");
            }
        }

    }


}



