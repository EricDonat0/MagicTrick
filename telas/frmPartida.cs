﻿using MagicTrick_piIII.classes;
using MagicTrick_piIII.Interfaces;
using MagicTrick_piIII.Telas;
using MagicTrickServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagicTrick_piIII.telas
{
    public partial class frmPartida : Form
    {
        List<Jogador> Jogadores = new List<Jogador>();
        Jogador Player;
        Partida Partida;
        Automato Automato;
        bool CartasImpressas = false;

        frmNarrador Narrador;
        frmStatus StatusForm;

        static int[,] posicoesNomes = { { 183, 183 }, { 419, 227 }, { 922, 183 }, { 419, 454 } };
       
        public frmPartida(Partida partida, List<Jogador> adversarios, Jogador player)
        {
            InitializeComponent();

            this.Partida = partida;
            this.Jogadores = adversarios;
            this.Jogadores.Add(player);
            this.Player = player;

            this.StatusForm = new frmStatus();
            this.Automato = new Automato(this.Player, this.Jogadores, this.StatusForm);

            AtualizarListaDeJogadores();
                               
            lblVersao.Text += Jogo.Versao;

            
            this.Narrador.Show();
            this.Narrador.NarrarPartidaNaoIniciada();

            this.StatusForm.Show();
        }

        private void AtualizarListaDeJogadores()
        {
            if(this.Narrador == null)
                this.Narrador = new frmNarrador();

            int idPartida = this.Partida.IdPartida;
            int idJogador = this.Player.IdJogador;

            List<Jogador> jogadoresTmp = Jogador.RetornarJogadoresPartida(idPartida);

            if (jogadoresTmp.Count == 0)
                return;

            if(this.Jogadores.Count != jogadoresTmp.Count)
            {
                this.Jogadores = jogadoresTmp;
                int indexJogador = this.Jogadores.FindIndex(j => j.IdJogador == idJogador);
                this.Jogadores[indexJogador] = this.Player;
            }

            this.Narrador.AtualizarJogadores(jogadoresTmp);
        }

        private void ExibirLabels()
        {
            int posicao;

            Label lblNome;
            Font fonteLabel = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            Point ponto;

            foreach (Jogador jogador in this.Jogadores)
            {
                posicao = (int)jogador.Posicao;
                lblNome = new Label();
                lblNome.Text = jogador.Nome;
                lblNome.Font = fonteLabel;
                lblNome.ForeColor = Color.White;
                lblNome.BackColor = Color.FromArgb(19, 23, 31);
                lblNome.Visible = true;

                ponto = new Point(posicoesNomes[posicao, 0], posicoesNomes[posicao, 1]);
                lblNome.Location = ponto;

                this.Controls.Add(lblNome);
                
                lblNome.BringToFront();
            }
        }

        private void AtualizarStatus(DadosVerificacao dadosVerificacao)
        {
            
            if(this.Partida.Round == 1 && this.Partida.Rodada == 1)
                AtualizarListaDeJogadores();
          
            this.Partida.Status = dadosVerificacao.StatusPartida;

            int idRetornado = dadosVerificacao.IdJogador;

            Jogador jogadorTmp;

            if (this.Player.IdJogador == idRetornado)
                jogadorTmp = this.Player;

            else
                jogadorTmp = this.Jogadores.Find(j => j.IdJogador == idRetornado);

            int rodadaAtual = dadosVerificacao.RodadaAtual;
           
            if (this.Partida.Rodada > rodadaAtual)
            {
                this.CartasImpressas = false;
                this.Partida.Round++;

                Jogador.AdicionarUltimoPontoDoRound(this.Jogadores, this.Partida);
                Jogador.AtualizarPlacares(this.Jogadores, this.Narrador);
            }

            this.Partida.Rodada = rodadaAtual;

            string nomeJogador = jogadorTmp.Nome;
            string tipoJogada = "Jogar carta";

            char statusRodada = dadosVerificacao.StatusRodada;
            this.Partida.StatusRodada = statusRodada;

            if (statusRodada == 'A')
                tipoJogada = "Apostar";

            string statusNovo = $"Vez do jogador: {nomeJogador}   -   ID: {idRetornado} : {tipoJogada}";
            lblStatusPartida.Text = statusNovo;
        }

        private bool ConsultarMao()
        {
            int idPartida = this.Partida.IdPartida;

            BaralhoConsulta cartas = BaralhoConsulta.HandleConsultarMao(idPartida);

            if (cartas.Baralho.Count == 0)
                return false;

            Control.ControlCollection controle = this.Controls;

            if (this.Partida.Round == 1)
            {
                Jogador.PreencherDeck(this.Jogadores, cartas, controle);
                this.Automato.InicializarPropriedades(ref this.Jogadores);
                this.Narrador.NarrarComecoDePartida();
            }

            else
            {
                Jogador.AtualizarDeck(this.Jogadores, cartas);
                this.Automato.ReiniciarPropriedades(ref this.Jogadores);
                this.Narrador.NarrarNovoRound(this.Partida.Round);
                this.StatusForm.LimparStatus();
            }

            return true;
        }

        private bool HandleVerificarVez()
        {
            int idPartida = this.Partida.IdPartida;
            int idJogador = this.Player.IdJogador;

            bool flagNovaRodada = false;

            DadosVerificacao verificacao = DadosVerificacao.RetornarDadosVerificacao(idPartida);

            this.Partida.DadosRodada = verificacao;

            if (verificacao == null)
            {                
                AtualizarListaDeJogadores();
                return false;
            }

            BaralhoVerificacao cartasRodada = verificacao.CartasRodada;

            //Verificações que demonstram que a partida finalizou:
            if (verificacao.StatusPartida == 'E' || verificacao.StatusPartida == 'F')
            {
                this.Partida.Status = verificacao.StatusPartida;

                this.ExibirPlacarFinal(verificacao.StatusPartida);
                return false;
            }

            this.StatusForm.AtualizarNaipeRodada(verificacao.NaipeRodada);

            CartaVerificacao cartaCampea = BaralhoVerificacao.RetornarCartaCampea(verificacao.CartasRodada);

            this.StatusForm.AtualizarCartaCampea(cartaCampea);

            if (this.Partida.Rodada != verificacao.RodadaAtual)
            {
                Jogador.EsconderCartasJogadas(this.Jogadores);
                flagNovaRodada = true;

                this.Narrador.NarrarNovaRodada(verificacao.RodadaAtual);
            }

            AtualizarStatus(verificacao);

            if (!this.CartasImpressas)
            {
                if(this.Partida.Round == 1)
                {                    
                    AtualizarListaDeJogadores();
                    Jogador.OrganizarJogadores(ref this.Jogadores, idJogador, this.Controls);
                    this.ExibirLabels();
                }

                if (!ConsultarMao())
                    return false;

                this.CartasImpressas = true;
                flagNovaRodada = false;                
            }

            Jogador.AtualizarJogadas(this.Jogadores, verificacao, this.Automato, this.Controls, this.Narrador);

            if (flagNovaRodada)
                Jogador.VerificarHistorico(this.Jogadores, this.Partida, this.Automato, this.Controls, this.Narrador);
            
            this.Partida.NaipeRodada = verificacao.NaipeRodada;

            this.Automato.LimitarCartas(cartasRodada);
            
            if (verificacao.IdJogador == this.Player.IdJogador)
                return true;

            else
                return false;
        }

        private void btnIniciarPartida_Click(object sender, EventArgs e)
        {
            int idJogador = this.Player.IdJogador;
            string senhaJogador = this.Player.Senha;

            string retorno = Jogo.IniciarPartida(idJogador, senhaJogador);

            if (Auxiliar.VerificarErro(retorno))
                return;

            btnIniciarPartida.Enabled = false;
            this.Partida.Status = 'J';

            int idRetornado = Convert.ToInt32(retorno);

            Jogador jogadorTmp;

            int idPartida = this.Partida.IdPartida;
            this.Jogadores = Jogador.RetornarJogadoresPartida(idPartida);

            int indexPlayer = this.Jogadores.FindIndex(j => j.IdJogador == idJogador);

            this.Jogadores[indexPlayer] = this.Player;

            if (this.Player.IdJogador == idRetornado)
                jogadorTmp = this.Player;

            else                
                jogadorTmp = this.Jogadores.Find(j => j.IdJogador == idRetornado);


            if (jogadorTmp == null)
                return;

            string nomeJogadorTmp = jogadorTmp.Nome;
            int idJogadorTmp = jogadorTmp.IdJogador;

            string statusNovo = $"Vez do jogador: {nomeJogadorTmp}   -   ID: {idJogadorTmp} : Jogar Carta";

            lblStatusPartida.Text = statusNovo;
        }       

        private bool Jogar(int posicao)
        {
            int idJogador = this.Player.IdJogador;
            string senha = this.Player.Senha;

            string retorno = "";

            if (this.Partida.StatusRodada == 'C')
            {
                try
                {
                    retorno = Jogo.Jogar(idJogador, senha, posicao);
                } 
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return this.Jogar(posicao);
                }

                if (Auxiliar.VerificarErro(retorno))
                    return false;
            }
            else
            {
                try
                {
                    retorno = Jogo.Apostar(idJogador, senha, posicao);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return this.Jogar(posicao);
                }

                if (Auxiliar.VerificarErro(retorno))
                    return false;
            }
            return true;
        }

        private void ExibirPlacarFinal(char statusPartida)
        {
            int idPartida = this.Partida.IdPartida;
            frmPlacarFinal placarFinal = new frmPlacarFinal(idPartida, statusPartida, this, this.Narrador, this.StatusForm);

            this.Narrador.NarrarFimDeJogo();

            tmrAtualizarEstado.Stop();
            placarFinal.Show();
        }

        private void tmrAtualizarEstado_Tick(object sender, EventArgs e)
        {
            int posicao;
            tmrAtualizarEstado.Enabled = false;

            bool vezDoPlayer = HandleVerificarVez();

            if (vezDoPlayer)
            {
                posicao = this.Automato.RetornarPosicaoEscolhida(this.Partida.DadosRodada);
                this.Jogar(posicao);
            }                                         
            
            if(this.Partida.Status != 'F' && this.Partida.Status != 'E')
                tmrAtualizarEstado.Enabled = true;
        }       
    }
}