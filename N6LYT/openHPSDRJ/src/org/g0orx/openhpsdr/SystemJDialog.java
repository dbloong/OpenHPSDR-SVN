/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package org.g0orx.openhpsdr;

/**
 *
 * @author john
 */
public class SystemJDialog extends javax.swing.JDialog {

    /**
     * Creates new form SystemJDialog
     */
    public SystemJDialog(java.awt.Frame parent, boolean modal) {
        super(parent, modal);
        initComponents();
        configuration=Configuration.getInstance();
        this.setTitle("Configuration");
        switch((int)configuration.samplerate) {
            case 48000:
                this.jRadioButtonRate48K.setSelected(true);
                break;
            case 96000:
                this.jRadioButtonRate96K.setSelected(true);
                break;
            case 192000:
                this.jRadioButtonRate192K.setSelected(true);
                break;
            case 384000:
                this.jRadioButtonRate384K.setSelected(true);
                break;
        }
        switch((int)configuration.buffersize) {
            case 64:
                this.jRadioButtonBuffer64.setSelected(true);
                break;
            case 128:
                this.jRadioButtonBuffer128.setSelected(true);
                break;
            case 256:
                this.jRadioButtonBuffer256.setSelected(true);
                break;
            case 512:
                this.jRadioButtonBuffer512.setSelected(true);
                break;
            case 1024:
                this.jRadioButtonBuffer1024.setSelected(true);
                break;
            case 2048:
                this.jRadioButtonBuffer2048.setSelected(true);
                break;
        }
        switch(configuration.fftsize) {
            case 256:
                this.jRadioButton256.setSelected(true);
                break;
            case 512:
                this.jRadioButton512.setSelected(true);
                break;
            case 1024:
                this.jRadioButton1024.setSelected(true);
                break;
            case 2048:
                this.jRadioButton2048.setSelected(true);
                break;
            case 4096:
                this.jRadioButton4096.setSelected(true);
                break;
            case 8192:
                this.jRadioButton8192.setSelected(true);
                break;
            case 16384:
                this.jRadioButton16384.setSelected(true);
                break;
        }
    }

    /**
     * This method is led from within the constructor to initialize the form.
     * WARNING: Do NOT modify this code. The content of this method is always
     * regenerated by the Form Editor.
     */
    @SuppressWarnings("unchecked")
    // <editor-fold defaultstate="collapsed" desc="Generated Code">//GEN-BEGIN:initComponents
    private void initComponents() {

        buttonGroupFFTSize = new javax.swing.ButtonGroup();
        buttonGroupRate = new javax.swing.ButtonGroup();
        buttonGroupBufferSize = new javax.swing.ButtonGroup();
        jLabel2 = new javax.swing.JLabel();
        jRadioButtonRate48K = new javax.swing.JRadioButton();
        jRadioButtonRate96K = new javax.swing.JRadioButton();
        jRadioButtonRate192K = new javax.swing.JRadioButton();
        jRadioButtonRate384K = new javax.swing.JRadioButton();
        jLabel3 = new javax.swing.JLabel();
        jRadioButton256 = new javax.swing.JRadioButton();
        jRadioButton512 = new javax.swing.JRadioButton();
        jRadioButton1024 = new javax.swing.JRadioButton();
        jRadioButton2048 = new javax.swing.JRadioButton();
        jRadioButton4096 = new javax.swing.JRadioButton();
        jRadioButton8192 = new javax.swing.JRadioButton();
        jButtonClose = new javax.swing.JButton();
        jLabel1 = new javax.swing.JLabel();
        jRadioButtonBuffer64 = new javax.swing.JRadioButton();
        jRadioButtonBuffer128 = new javax.swing.JRadioButton();
        jRadioButtonBuffer256 = new javax.swing.JRadioButton();
        jRadioButtonBuffer512 = new javax.swing.JRadioButton();
        jRadioButtonBuffer1024 = new javax.swing.JRadioButton();
        jRadioButton16384 = new javax.swing.JRadioButton();
        jRadioButtonBuffer2048 = new javax.swing.JRadioButton();

        setDefaultCloseOperation(javax.swing.WindowConstants.DISPOSE_ON_CLOSE);

        jLabel2.setText("Sample Rate:");

        buttonGroupRate.add(jRadioButtonRate48K);
        jRadioButtonRate48K.setText("48K");
        jRadioButtonRate48K.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonRate48KActionPerformed(evt);
            }
        });

        buttonGroupRate.add(jRadioButtonRate96K);
        jRadioButtonRate96K.setText("96K");
        jRadioButtonRate96K.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonRate96KActionPerformed(evt);
            }
        });

        buttonGroupRate.add(jRadioButtonRate192K);
        jRadioButtonRate192K.setText("192K");
        jRadioButtonRate192K.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonRate192KActionPerformed(evt);
            }
        });

        buttonGroupRate.add(jRadioButtonRate384K);
        jRadioButtonRate384K.setText("384K");
        jRadioButtonRate384K.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonRate384KActionPerformed(evt);
            }
        });

        jLabel3.setText("FFT Size:");

        buttonGroupFFTSize.add(jRadioButton256);
        jRadioButton256.setText("256");
        jRadioButton256.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton256ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton512);
        jRadioButton512.setText("512");
        jRadioButton512.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton512ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton1024);
        jRadioButton1024.setText("1024");
        jRadioButton1024.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton1024ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton2048);
        jRadioButton2048.setText("2048");
        jRadioButton2048.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton2048ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton4096);
        jRadioButton4096.setText("4096");
        jRadioButton4096.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton4096ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton8192);
        jRadioButton8192.setText("8192");
        jRadioButton8192.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton8192ActionPerformed(evt);
            }
        });

        jButtonClose.setText("Close");
        jButtonClose.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jButtonCloseActionPerformed(evt);
            }
        });

        jLabel1.setText("Buffer Size:");

        buttonGroupBufferSize.add(jRadioButtonBuffer64);
        jRadioButtonBuffer64.setText("64");
        jRadioButtonBuffer64.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer64ActionPerformed(evt);
            }
        });

        buttonGroupBufferSize.add(jRadioButtonBuffer128);
        jRadioButtonBuffer128.setText("128");
        jRadioButtonBuffer128.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer128ActionPerformed(evt);
            }
        });

        buttonGroupBufferSize.add(jRadioButtonBuffer256);
        jRadioButtonBuffer256.setText("256");
        jRadioButtonBuffer256.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer256ActionPerformed(evt);
            }
        });

        buttonGroupBufferSize.add(jRadioButtonBuffer512);
        jRadioButtonBuffer512.setText("512");
        jRadioButtonBuffer512.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer512ActionPerformed(evt);
            }
        });

        buttonGroupBufferSize.add(jRadioButtonBuffer1024);
        jRadioButtonBuffer1024.setText("1024");
        jRadioButtonBuffer1024.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer1024ActionPerformed(evt);
            }
        });

        buttonGroupFFTSize.add(jRadioButton16384);
        jRadioButton16384.setText("16384");
        jRadioButton16384.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButton16384ActionPerformed(evt);
            }
        });

        buttonGroupBufferSize.add(jRadioButtonBuffer2048);
        jRadioButtonBuffer2048.setText("2048");
        jRadioButtonBuffer2048.addActionListener(new java.awt.event.ActionListener() {
            public void actionPerformed(java.awt.event.ActionEvent evt) {
                jRadioButtonBuffer2048ActionPerformed(evt);
            }
        });

        javax.swing.GroupLayout layout = new javax.swing.GroupLayout(getContentPane());
        getContentPane().setLayout(layout);
        layout.setHorizontalGroup(
            layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGroup(layout.createSequentialGroup()
                .addGap(25, 25, 25)
                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                    .addComponent(jLabel2)
                    .addComponent(jRadioButtonRate48K)
                    .addComponent(jRadioButtonRate96K)
                    .addComponent(jRadioButtonRate192K)
                    .addComponent(jRadioButtonRate384K))
                .addGap(39, 39, 39)
                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                    .addComponent(jButtonClose)
                    .addGroup(layout.createSequentialGroup()
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                            .addComponent(jRadioButtonBuffer256)
                            .addComponent(jRadioButtonBuffer512)
                            .addComponent(jRadioButtonBuffer1024)
                            .addComponent(jRadioButtonBuffer128)
                            .addComponent(jRadioButtonBuffer64)
                            .addComponent(jRadioButtonBuffer2048)
                            .addComponent(jLabel1))
                        .addGap(33, 33, 33)
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                            .addComponent(jLabel3)
                            .addComponent(jRadioButton16384)
                            .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.TRAILING)
                                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                                    .addComponent(jRadioButton512)
                                    .addComponent(jRadioButton256)
                                    .addComponent(jRadioButton1024))
                                .addComponent(jRadioButton2048)
                                .addComponent(jRadioButton4096)
                                .addComponent(jRadioButton8192)))))
                .addContainerGap(30, Short.MAX_VALUE))
        );
        layout.setVerticalGroup(
            layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
            .addGroup(layout.createSequentialGroup()
                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.LEADING)
                    .addGroup(layout.createSequentialGroup()
                        .addGap(35, 35, 35)
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                            .addComponent(jRadioButtonRate48K)
                            .addComponent(jRadioButtonBuffer64))
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                            .addComponent(jRadioButtonRate96K)
                            .addComponent(jRadioButtonBuffer128))
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                            .addComponent(jRadioButtonRate192K)
                            .addComponent(jRadioButtonBuffer256))
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                            .addComponent(jRadioButtonRate384K)
                            .addComponent(jRadioButtonBuffer512)))
                    .addGroup(layout.createSequentialGroup()
                        .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                            .addComponent(jLabel1)
                            .addComponent(jLabel2)
                            .addComponent(jLabel3, javax.swing.GroupLayout.PREFERRED_SIZE, 28, javax.swing.GroupLayout.PREFERRED_SIZE))
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addComponent(jRadioButton256)
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addComponent(jRadioButton512)
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addComponent(jRadioButton1024)
                        .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                        .addComponent(jRadioButton2048)))
                .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                    .addComponent(jRadioButtonBuffer1024)
                    .addComponent(jRadioButton4096, javax.swing.GroupLayout.PREFERRED_SIZE, 18, javax.swing.GroupLayout.PREFERRED_SIZE))
                .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                .addGroup(layout.createParallelGroup(javax.swing.GroupLayout.Alignment.BASELINE)
                    .addComponent(jRadioButtonBuffer2048)
                    .addComponent(jRadioButton8192))
                .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED)
                .addComponent(jRadioButton16384)
                .addPreferredGap(javax.swing.LayoutStyle.ComponentPlacement.RELATED, 29, Short.MAX_VALUE)
                .addComponent(jButtonClose)
                .addContainerGap())
        );

        pack();
    }// </editor-fold>//GEN-END:initComponents

    private void jRadioButtonRate48KActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonRate48KActionPerformed
        configuration.samplerate=48000;
    }//GEN-LAST:event_jRadioButtonRate48KActionPerformed

    private void jRadioButtonRate96KActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonRate96KActionPerformed
        configuration.samplerate=96000;
    }//GEN-LAST:event_jRadioButtonRate96KActionPerformed

    private void jRadioButtonRate192KActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonRate192KActionPerformed
        configuration.samplerate=192000;
    }//GEN-LAST:event_jRadioButtonRate192KActionPerformed

    private void jRadioButtonRate384KActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonRate384KActionPerformed
        configuration.samplerate=384000;
    }//GEN-LAST:event_jRadioButtonRate384KActionPerformed

    private void jRadioButton256ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton256ActionPerformed
        configuration.fftsize=256;
    }//GEN-LAST:event_jRadioButton256ActionPerformed

    private void jRadioButton4096ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton4096ActionPerformed
        configuration.fftsize=4096;
    }//GEN-LAST:event_jRadioButton4096ActionPerformed

    private void jRadioButton512ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton512ActionPerformed
        configuration.fftsize=512;
    }//GEN-LAST:event_jRadioButton512ActionPerformed

    private void jRadioButton1024ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton1024ActionPerformed
        configuration.fftsize=1024;
    }//GEN-LAST:event_jRadioButton1024ActionPerformed

    private void jRadioButton2048ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton2048ActionPerformed
        configuration.fftsize=2048;
    }//GEN-LAST:event_jRadioButton2048ActionPerformed

    private void jRadioButton8192ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton8192ActionPerformed
        configuration.fftsize=8192;
    }//GEN-LAST:event_jRadioButton8192ActionPerformed

    private void jButtonCloseActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jButtonCloseActionPerformed
        this.dispose();
    }//GEN-LAST:event_jButtonCloseActionPerformed

    private void jRadioButton16384ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButton16384ActionPerformed
        configuration.fftsize=16384;
    }//GEN-LAST:event_jRadioButton16384ActionPerformed

    private void jRadioButtonBuffer64ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer64ActionPerformed
        configuration.buffersize=64;
    }//GEN-LAST:event_jRadioButtonBuffer64ActionPerformed

    private void jRadioButtonBuffer128ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer128ActionPerformed
        configuration.buffersize=128;
    }//GEN-LAST:event_jRadioButtonBuffer128ActionPerformed

    private void jRadioButtonBuffer256ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer256ActionPerformed
        configuration.buffersize=256;
    }//GEN-LAST:event_jRadioButtonBuffer256ActionPerformed

    private void jRadioButtonBuffer512ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer512ActionPerformed
        configuration.buffersize=512;
    }//GEN-LAST:event_jRadioButtonBuffer512ActionPerformed

    private void jRadioButtonBuffer1024ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer1024ActionPerformed
        configuration.buffersize=1024;
    }//GEN-LAST:event_jRadioButtonBuffer1024ActionPerformed

    private void jRadioButtonBuffer2048ActionPerformed(java.awt.event.ActionEvent evt) {//GEN-FIRST:event_jRadioButtonBuffer2048ActionPerformed
        configuration.buffersize=2048;
    }//GEN-LAST:event_jRadioButtonBuffer2048ActionPerformed
    
    private Configuration configuration;
    
    // Variables declaration - do not modify//GEN-BEGIN:variables
    private javax.swing.ButtonGroup buttonGroupBufferSize;
    private javax.swing.ButtonGroup buttonGroupFFTSize;
    private javax.swing.ButtonGroup buttonGroupRate;
    private javax.swing.JButton jButtonClose;
    private javax.swing.JLabel jLabel1;
    private javax.swing.JLabel jLabel2;
    private javax.swing.JLabel jLabel3;
    private javax.swing.JRadioButton jRadioButton1024;
    private javax.swing.JRadioButton jRadioButton16384;
    private javax.swing.JRadioButton jRadioButton2048;
    private javax.swing.JRadioButton jRadioButton256;
    private javax.swing.JRadioButton jRadioButton4096;
    private javax.swing.JRadioButton jRadioButton512;
    private javax.swing.JRadioButton jRadioButton8192;
    private javax.swing.JRadioButton jRadioButtonBuffer1024;
    private javax.swing.JRadioButton jRadioButtonBuffer128;
    private javax.swing.JRadioButton jRadioButtonBuffer2048;
    private javax.swing.JRadioButton jRadioButtonBuffer256;
    private javax.swing.JRadioButton jRadioButtonBuffer512;
    private javax.swing.JRadioButton jRadioButtonBuffer64;
    private javax.swing.JRadioButton jRadioButtonRate192K;
    private javax.swing.JRadioButton jRadioButtonRate384K;
    private javax.swing.JRadioButton jRadioButtonRate48K;
    private javax.swing.JRadioButton jRadioButtonRate96K;
    // End of variables declaration//GEN-END:variables
}