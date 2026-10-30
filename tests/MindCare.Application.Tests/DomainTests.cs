using MindCare.Application.Common;
using MindCare.Application.Domain;

namespace MindCare.Application.Tests;

public class PaymentTests
{
    [Theory]
    [InlineData(150, 0, PaymentStatus.Pendente)]
    [InlineData(150, 50, PaymentStatus.Parcial)]
    [InlineData(150, 150, PaymentStatus.Confirmado)]
    [InlineData(0, 0, PaymentStatus.Confirmado)]
    public void ResolveStatus_deriva_status_dos_valores(decimal price, decimal paid, PaymentStatus expected) =>
        Assert.Equal(expected, Payment.ResolveStatus(price, paid));

    [Fact]
    public void Reconcile_define_data_de_hoje_quando_ha_valor_pago()
    {
        var payment = new Payment { Price = 200, PaidPrice = 80 };

        payment.Reconcile(new DateTime(2026, 3, 10, 15, 30, 0));

        Assert.Equal(PaymentStatus.Parcial, payment.Status);
        Assert.Equal(new DateTime(2026, 3, 10), payment.PaidDate);
        Assert.Equal(120, payment.Outstanding);
    }

    [Fact]
    public void Reconcile_preserva_data_informada()
    {
        var payment = new Payment { Price = 200, PaidPrice = 200, PaidDate = new DateTime(2026, 1, 5) };

        payment.Reconcile(new DateTime(2026, 3, 10));

        Assert.Equal(new DateTime(2026, 1, 5), payment.PaidDate);
    }

    [Fact]
    public void Reconcile_limpa_data_quando_nada_foi_pago()
    {
        var payment = new Payment { Price = 200, PaidPrice = 0, PaidDate = new DateTime(2026, 1, 5) };

        payment.Reconcile(new DateTime(2026, 3, 10));

        Assert.Null(payment.PaidDate);
        Assert.Equal(PaymentStatus.Pendente, payment.Status);
    }
}

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void IsValid_aceita_cpfs_validos(string cpf) => Assert.True(Cpf.IsValid(cpf));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("1234567891")]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    public void IsValid_rejeita_cpfs_invalidos(string? cpf) => Assert.False(Cpf.IsValid(cpf));

    [Fact]
    public void Normalize_e_Format_sao_inversos() =>
        Assert.Equal("529.982.247-25", Cpf.Format(Cpf.Normalize("529.982.247-25")));
}
