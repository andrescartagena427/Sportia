using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Sportia.Models;

public partial class SportiaDbContext : DbContext
{
    public SportiaDbContext()
    {
    }

    public SportiaDbContext(DbContextOptions<SportiaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<DisponibilidadCancha> DisponibilidadCanchas { get; set; }

    public virtual DbSet<Empresa> Empresas { get; set; }

    public virtual DbSet<Escenario> Escenarios { get; set; }

    public virtual DbSet<EstadosReserva> EstadosReservas { get; set; }

    public virtual DbSet<MetodosPago> MetodosPagos { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tarifa> Tarifas { get; set; }

    public virtual DbSet<TiposEscenario> TiposEscenarios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=ConexionSportia", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.43-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_spanish_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("PRIMARY");

            entity.ToTable("clientes");

            entity.HasIndex(e => e.Documento, "documento").IsUnique();

            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .HasColumnName("correo");
            entity.Property(e => e.Documento)
                .HasMaxLength(20)
                .HasColumnName("documento");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<DisponibilidadCancha>(entity =>
        {
            entity.HasKey(e => e.IdDisponibilidad).HasName("PRIMARY");

            entity.ToTable("disponibilidad_cancha");

            entity.HasIndex(e => e.IdEscenario, "id_escenario");

            entity.Property(e => e.IdDisponibilidad).HasColumnName("id_disponibilidad");
            entity.Property(e => e.DiaSemana)
                .HasColumnType("enum('Lunes','Martes','Miércoles','Jueves','Viernes','Sábado','Domingo')")
                .HasColumnName("dia_semana");
            entity.Property(e => e.HoraApertura)
                .HasColumnType("time")
                .HasColumnName("hora_apertura");
            entity.Property(e => e.HoraCierre)
                .HasColumnType("time")
                .HasColumnName("hora_cierre");
            entity.Property(e => e.IdEscenario).HasColumnName("id_escenario");

            entity.HasOne(d => d.IdEscenarioNavigation).WithMany(p => p.DisponibilidadCanchas)
                .HasForeignKey(d => d.IdEscenario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("disponibilidad_cancha_ibfk_1");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.IdEmpresa).HasName("PRIMARY");

            entity.ToTable("empresas");

            entity.HasIndex(e => e.IdUsuario, "id_usuario");

            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Correo)
                .HasMaxLength(120)
                .HasColumnName("correo");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Logo)
                .HasMaxLength(255)
                .HasColumnName("logo");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .HasColumnName("nit");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Empresas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("empresas_ibfk_1");
        });

        modelBuilder.Entity<Escenario>(entity =>
        {
            entity.HasKey(e => e.IdEscenario).HasName("PRIMARY");

            entity.ToTable("escenarios");

            entity.HasIndex(e => e.IdEmpresa, "id_empresa");

            entity.HasIndex(e => e.IdTipo, "id_tipo");

            entity.Property(e => e.IdEscenario).HasColumnName("id_escenario");
            entity.Property(e => e.Capacidad).HasColumnName("capacidad");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.IdTipo).HasColumnName("id_tipo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Escenarios)
                .HasForeignKey(d => d.IdEmpresa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("escenarios_ibfk_1");

            entity.HasOne(d => d.IdTipoNavigation).WithMany(p => p.Escenarios)
                .HasForeignKey(d => d.IdTipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("escenarios_ibfk_2");
        });

        modelBuilder.Entity<EstadosReserva>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PRIMARY");

            entity.ToTable("estados_reserva");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<MetodosPago>(entity =>
        {
            entity.HasKey(e => e.IdMetodo).HasName("PRIMARY");

            entity.ToTable("metodos_pago");

            entity.Property(e => e.IdMetodo).HasColumnName("id_metodo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.IdPago).HasName("PRIMARY");

            entity.ToTable("pagos");

            entity.HasIndex(e => e.IdMetodo, "id_metodo");

            entity.HasIndex(e => e.IdReserva, "id_reserva");

            entity.Property(e => e.IdPago).HasColumnName("id_pago");
            entity.Property(e => e.Comprobante)
                .HasMaxLength(255)
                .HasColumnName("comprobante");
            entity.Property(e => e.FechaPago)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_pago");
            entity.Property(e => e.IdMetodo).HasColumnName("id_metodo");
            entity.Property(e => e.IdReserva).HasColumnName("id_reserva");
            entity.Property(e => e.MontoPagado)
                .HasPrecision(10, 2)
                .HasColumnName("monto_pagado");
            entity.Property(e => e.SaldoPendiente)
                .HasPrecision(10, 2)
                .HasColumnName("saldo_pendiente");

            entity.HasOne(d => d.IdMetodoNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdMetodo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pagos_ibfk_2");

            entity.HasOne(d => d.IdReservaNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdReserva)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pagos_ibfk_1");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.IdReserva).HasName("PRIMARY");

            entity.ToTable("reservas");

            entity.HasIndex(e => e.Codigo, "codigo").IsUnique();

            entity.HasIndex(e => e.IdCliente, "id_cliente");

            entity.HasIndex(e => e.IdEscenario, "id_escenario");

            entity.HasIndex(e => e.IdEstado, "id_estado");

            entity.HasIndex(e => e.IdUsuario, "id_usuario");

            entity.Property(e => e.IdReserva).HasColumnName("id_reserva");
            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .HasColumnName("codigo");
            entity.Property(e => e.FechaReserva)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_reserva");
            entity.Property(e => e.FechaUso).HasColumnName("fecha_uso");
            entity.Property(e => e.HoraFin)
                .HasColumnType("time")
                .HasColumnName("hora_fin");
            entity.Property(e => e.HoraInicio)
                .HasColumnType("time")
                .HasColumnName("hora_inicio");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdEscenario).HasColumnName("id_escenario");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");
            entity.Property(e => e.ValorTotal)
                .HasPrecision(10, 2)
                .HasColumnName("valor_total");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reservas_ibfk_2");

            entity.HasOne(d => d.IdEscenarioNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdEscenario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reservas_ibfk_1");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reservas_ibfk_4");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("reservas_ibfk_3");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Tarifa>(entity =>
        {
            entity.HasKey(e => e.IdTarifa).HasName("PRIMARY");

            entity.ToTable("tarifas");

            entity.HasIndex(e => e.IdEscenario, "id_escenario");

            entity.Property(e => e.IdTarifa).HasColumnName("id_tarifa");
            entity.Property(e => e.DiaSemana)
                .HasMaxLength(20)
                .HasColumnName("dia_semana");
            entity.Property(e => e.HoraFin)
                .HasColumnType("time")
                .HasColumnName("hora_fin");
            entity.Property(e => e.HoraInicio)
                .HasColumnType("time")
                .HasColumnName("hora_inicio");
            entity.Property(e => e.IdEscenario).HasColumnName("id_escenario");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");

            entity.HasOne(d => d.IdEscenarioNavigation).WithMany(p => p.Tarifas)
                .HasForeignKey(d => d.IdEscenario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tarifas_ibfk_1");
        });

        modelBuilder.Entity<TiposEscenario>(entity =>
        {
            entity.HasKey(e => e.IdTipo).HasName("PRIMARY");

            entity.ToTable("tipos_escenario");

            entity.Property(e => e.IdTipo).HasColumnName("id_tipo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.HasIndex(e => e.Documento, "documento").IsUnique();

            entity.HasIndex(e => e.IdRol, "id_rol");

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.Correo)
                .HasMaxLength(120)
                .HasColumnName("correo");
            entity.Property(e => e.Documento)
                .HasMaxLength(20)
                .HasColumnName("documento");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
