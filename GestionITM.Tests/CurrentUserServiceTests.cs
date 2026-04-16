using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using Moq;
using Itm.Inventory.Api.Services;
using System.Security.Cryptography.X509Certificates;

namespace Itm.Inventory.Tests.Services
{
    public class CurrentUserServiceTests
    {
        // Prueba 1: El camino feliz (Validar que extrae el correo del JWT correctamente)
        [Fact] // Este atributo le dice a Visual Studio que este método es una prueba unitaria
        public void ObtenerEmailUsuario_ConTokenValido_DebeRetornarEmail()
        {
            // 1. Arrange: preparamos el escenario de prueba

            // Creamos el "Doble de acción" (Mock) de la interfaz de .Net que representa el contexto HTTP
            // Por eso es vital inyectar IHttpContextAccessor en el constructor de CurrentUserService, para poder simular el contexto HTTP en las pruebas

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Preparamos los "tomates de plástico"  (Un usuario en memoria falso)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "estudiante@itm.edu..co") // Simulamos que el JWT tiene un claim de email con este valor
            };

            var identity = new ClaimsIdentity(claims, "TestAuth"); // Creamos una identidad con esos claims
            var claimsPrincipal = new ClaimsPrincipal(identity); // Creamos un ClaimsPrincipal con esa identidad

            // Simulamos el objeto http context para que tenga ese usuario autenticado
            var contextoFalso = new DefaultHttpContext
            {
                User = claimsPrincipal // Asignamos el usuario simulado al contexto HTTP falso
            };

            // lE DECIMOS AL MOCK CÓMO DSEBE ACTUAR CUANDO SE LE PIDA EL CONTEXTO HTTP
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(contextoFalso);

            // Instanciamos el servicio REAL, pero le inyectamos el Mock falso
            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            //2. ACT: Ejecutamos la acción que queremos probar

            var resultado = currentUserService.ObtenerEmailUsuario();

            // 3. ASSERT: Verificamos que el resultado sea el esperado

            // Exigimos que el Email extraido sea exactamente igual al que pusimos en el mock o claim del JWT simulado

            Assert.Equal("estudiante@itm.edu..co", resultado);
        }

        // Prueba 2: El camino triste (Validar que si no hay token o el token no tiene el claim de email, se maneje correctamente)
        [Fact]
        public void ObtenerEmailUsuario_SinContextoHttp_DebeRetornarStringVacia()
        {
            // 1. Arrange
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            // Simulamos que el HttpContext es nulo, lo que podría ocurrir si el servicio se usa en
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var currentUserService = new CurrentUserService(mockHttpContextAccessor.Object);

            // 2. Act
            var resultado = currentUserService.ObtenerEmailUsuario();

            // 3. Assert

            Assert.Equal(string.Empty, resultado); // Esperamos que retorne una cadena vacía si no hay contexto HTTP
        }
    }
}

